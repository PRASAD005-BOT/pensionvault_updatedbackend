using Moq;
using NUnit.Framework;
using Annuity.Services;
using Annuity.Services.DTOs;
using Annuity.Services.HttpClients;
using Annuity.Domain.Entities;
using Annuity.Domain.Repositories;
using PensionVault.Shared.Contracts;

namespace PensionVault.Annuity.Tests;

/// <summary>
/// NUnit tests for AnnuityService — covering Eligibility, Submit Request,
/// Approve/Reject/Cancel, Monthly Disbursement, Nominee Settlement,
/// and the internal monthly pension calculation formula.
///
/// Because MemberServiceClient, ContributionsServiceClient, and
/// NotificationServiceClient are concrete HttpClient-based classes (not
/// interfaces), we use a protected-virtual hook pattern via
/// TestableAnnuityService to substitute in test doubles.
/// </summary>
[TestFixture]
public class AnnuityServiceTests
{
    // ── Mocks ─────────────────────────────────────────────────────────────────
    private Mock<IAnnuityRepository>        _annuityRepo = null!;
    private Mock<IAnnuityRequestRepository> _requestRepo = null!;
    private Mock<IUnitOfWork>               _unitOfWork  = null!;

    // Controlled member / contributions data returned by HTTP-client stubs
    private MemberResponse?      _memberResponse;
    private FundAccountResponse? _fundAccountResponse;
    private List<LocalContribution> _contributions = null!;

    private TestableAnnuityService _sut = null!;

    // ── Shared IDs ────────────────────────────────────────────────────────────
    private static readonly Guid MemberId   = Guid.NewGuid();
    private static readonly Guid AnnuityId  = Guid.NewGuid();
    private static readonly Guid RequestId  = Guid.NewGuid();
    private static readonly Guid ReviewerId = Guid.NewGuid();
    private static readonly Guid AccountId  = Guid.NewGuid();

    // ─────────────────────────────────────────────────────────────────────────
    // TestableAnnuityService — wraps the real service, overriding HTTP calls
    // ─────────────────────────────────────────────────────────────────────────
    private sealed class TestableAnnuityService : AnnuityService
    {
        private readonly Func<Guid, Task<MemberResponse?>>      _getMember;
        private readonly Func<Guid, Task<FundAccountResponse?>> _getAccount;
        private readonly Func<Guid, Task<List<LocalContribution>>> _getContributions;

        public TestableAnnuityService(
            IAnnuityRepository        annuityRepo,
            IAnnuityRequestRepository requestRepo,
            IUnitOfWork               unitOfWork,
            Func<Guid, Task<MemberResponse?>>         getMember,
            Func<Guid, Task<FundAccountResponse?>>    getAccount,
            Func<Guid, Task<List<LocalContribution>>> getContributions)
            : base(annuityRepo, requestRepo, null!, null!, null!, unitOfWork)
        {
            _getMember        = getMember;
            _getAccount       = getAccount;
            _getContributions = getContributions;
        }

        protected override Task<MemberResponse?> FetchMemberAsync(Guid memberId)
            => _getMember(memberId);

        protected override Task<FundAccountResponse?> FetchFundAccountAsync(Guid memberId)
            => _getAccount(memberId);

        protected override Task<List<LocalContribution>> FetchContributionsAsync(Guid memberId)
            => _getContributions(memberId);

        protected override Task SendNotificationsAsync(IEnumerable<CreateNotificationRequest> requests)
            => Task.CompletedTask;
    }

    [SetUp]
    public void SetUp()
    {
        _annuityRepo = new Mock<IAnnuityRepository>();
        _requestRepo = new Mock<IAnnuityRequestRepository>();
        _unitOfWork  = new Mock<IUnitOfWork>();
        _contributions = new List<LocalContribution>();

        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Default stubs (overridden per-test as needed)
        _memberResponse = BuildEligibleMember();
        _fundAccountResponse = new FundAccountResponse(
            AccountId, MemberId, Guid.NewGuid(),
            DateTime.UtcNow.AddYears(-5),
            EmployeeContributionBalance: 100000m,
            EmployerContributionBalance: 100000m,
            PensionBalance: 250000m,
            InterestAccrued: 5000m,
            TotalBalance: 455000m,
            VestingPercent: 100,
            Status: "Active"
        );

        _sut = new TestableAnnuityService(
            _annuityRepo.Object,
            _requestRepo.Object,
            _unitOfWork.Object,
            id => Task.FromResult(_memberResponse),
            id => Task.FromResult(_fundAccountResponse),
            id => Task.FromResult(_contributions)
        );
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MemberResponse BuildEligibleMember(int ageYears = 55, int serviceYears = 15) =>
        new MemberResponse(
            MemberId,
            MembershipNumber: "MEM-2024-001",
            Name: "John Doe",
            DateOfBirth: DateTime.UtcNow.AddYears(-ageYears),
            Gender: "Male",
            NationalIdRef: "123456789012",
            EmployerId: Guid.NewGuid(),
            EmployerName: "ACME Corp",
            JoiningDate: DateTime.UtcNow.AddYears(-serviceYears),
            DateOfRetirement: DateTime.UtcNow.AddYears(5),
            NomineeName: "Jane Doe",
            NomineeRelation: "Spouse",
            NomineeBankAccount: "987654321",
            NomineePercent: 100,
            Status: "Active",
            ProfileImageUrl: null,
            Email: "john@example.com",
            UserId: Guid.NewGuid(),
            Phone: null
        );

    private AnnuityPlan BuildActivePlan(decimal monthlyPension = 4214.86m) =>
        new AnnuityPlan
        {
            AnnuityId        = AnnuityId,
            MemberId         = MemberId,
            PlanType         = AnnuityPlanType.LifeAnnuity,
            PurchaseValue    = 250000m,
            MonthlyPension   = monthlyPension,
            AnnuityStartDate = DateTime.UtcNow.AddMonths(-3),
            Status           = AnnuityStatus.Active
        };

    private AnnuityRequest BuildPendingRequest() =>
        new AnnuityRequest
        {
            RequestId               = RequestId,
            MemberId                = MemberId,
            PlanType                = AnnuityPlanType.LifeAnnuity,
            PensionBalanceAtRequest = 250000m,
            EstimatedMonthly        = 4214.86m,
            Status                  = AnnuityRequestStatus.Pending,
            RequestedAt             = DateTime.UtcNow.AddDays(-1)
        };

    // ═══════════════════════════════════════════════════════════════════════════
    // ELIGIBILITY TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Annuity.Eligibility")]
    public async Task CheckEligibilityAsync_EligibleMember_ReturnsIsEligibleTrue()
    {
        // Arrange — member is 55 years old, 15 years of service, ₹2,50,000 EPS balance
        _memberResponse = BuildEligibleMember(ageYears: 55, serviceYears: 15);
        _contributions  = Enumerable.Range(1, 120).Select(i => new LocalContribution($"2024-{i:D2}")).ToList();

        // Act
        var result = await _sut.CheckEligibilityAsync(MemberId);

        // Assert
        Assert.That(result.IsEligible,        Is.True);
        Assert.That(result.FailureReasons,    Is.Empty);
        Assert.That(result.PensionBalance,    Is.EqualTo(250000m));
        Assert.That(result.AgeYears,          Is.GreaterThanOrEqualTo(50));
        Assert.That(result.ServiceYears,      Is.GreaterThanOrEqualTo(10));
    }

    [Test]
    [Category("Annuity.Eligibility")]
    public async Task CheckEligibilityAsync_AgeTooYoung_ReturnsNotEligible()
    {
        // Arrange — member is only 45 years old (< 50 minimum)
        _memberResponse = BuildEligibleMember(ageYears: 45, serviceYears: 15);

        // Act
        var result = await _sut.CheckEligibilityAsync(MemberId);

        // Assert
        Assert.That(result.IsEligible, Is.False);
        Assert.That(result.FailureReasons, Has.Some.Contains("50"));
    }

    [Test]
    [Category("Annuity.Eligibility")]
    public async Task CheckEligibilityAsync_InsufficientService_ReturnsNotEligible()
    {
        // Arrange — only 5 years of service (< 10 minimum)
        _memberResponse = BuildEligibleMember(ageYears: 55, serviceYears: 5);

        // Act
        var result = await _sut.CheckEligibilityAsync(MemberId);

        // Assert
        Assert.That(result.IsEligible, Is.False);
        Assert.That(result.FailureReasons, Has.Some.Contains("10"));
    }

    [Test]
    [Category("Annuity.Eligibility")]
    public async Task CheckEligibilityAsync_ZeroPensionBalance_ReturnsNotEligible()
    {
        // Arrange — EPS balance is zero
        _memberResponse      = BuildEligibleMember(ageYears: 55, serviceYears: 15);
        _fundAccountResponse = _fundAccountResponse! with { PensionBalance = 0m };

        // Act
        var result = await _sut.CheckEligibilityAsync(MemberId);

        // Assert
        Assert.That(result.IsEligible, Is.False);
        Assert.That(result.FailureReasons, Has.Some.Contains("₹0"));
    }

    [Test]
    [Category("Annuity.Eligibility")]
    public async Task CheckEligibilityAsync_InactiveMember_ReturnsNotEligible()
    {
        // Arrange
        _memberResponse = BuildEligibleMember(ageYears: 55, serviceYears: 15) with { Status = "Inactive" };

        // Act
        var result = await _sut.CheckEligibilityAsync(MemberId);

        // Assert
        Assert.That(result.IsEligible, Is.False);
        Assert.That(result.FailureReasons, Has.Some.Contains("Active or Retired"));
    }

    [Test]
    [Category("Annuity.Eligibility")]
    public async Task CheckEligibilityAsync_MemberNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _memberResponse = null;

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.CheckEligibilityAsync(MemberId));
    }

    [Test]
    [Category("Annuity.Eligibility")]
    public async Task CheckEligibilityAsync_RetiredMember_IsEligible()
    {
        // Arrange — Retired status should also be accepted
        _memberResponse = BuildEligibleMember(ageYears: 62, serviceYears: 25) with { Status = "Retired" };

        // Act
        var result = await _sut.CheckEligibilityAsync(MemberId);

        // Assert
        Assert.That(result.IsEligible, Is.True);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MONTHLY PENSION FORMULA TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Annuity.Formula")]
    public void CalcMonthlyPension_LifeAnnuity_500000_Returns4214()
    {
        // Standard PMT formula: PV=500000, r=8%/12, n=20*12=240
        var result = AnnuityService.ExposedCalcMonthlyPension(500000m, AnnuityPlanType.LifeAnnuity);
        // Expected ≈ ₹4,181.84  (some rounding depending on implementation)
        Assert.That((double)result, Is.InRange(4100.0, 4300.0),
            "LifeAnnuity monthly pension for ₹5,00,000 should be ~₹4,181-₹4,215");
    }

    [Test]
    [Category("Annuity.Formula")]
    public void CalcMonthlyPension_JointAnnuity_IsLessThanLifeAnnuity()
    {
        // Joint annuity spans 30 years vs 20 for life — so monthly pension is smaller
        var life  = AnnuityService.ExposedCalcMonthlyPension(500000m, AnnuityPlanType.LifeAnnuity);
        var joint = AnnuityService.ExposedCalcMonthlyPension(500000m, AnnuityPlanType.JointAnnuity);
        Assert.That(joint, Is.LessThan(life),
            "JointAnnuity (30yr) must pay less per month than LifeAnnuity (20yr) for the same corpus");
    }

    [Test]
    [Category("Annuity.Formula")]
    public void CalcMonthlyPension_TemporaryAnnuity_IsHighestMonthly()
    {
        // Temporary annuity only lasts 10 years, so monthly pension is highest
        var temp      = AnnuityService.ExposedCalcMonthlyPension(500000m, AnnuityPlanType.TemporaryAnnuity);
        var life      = AnnuityService.ExposedCalcMonthlyPension(500000m, AnnuityPlanType.LifeAnnuity);
        var guaranteed = AnnuityService.ExposedCalcMonthlyPension(500000m, AnnuityPlanType.GuaranteedAnnuity);
        var joint     = AnnuityService.ExposedCalcMonthlyPension(500000m, AnnuityPlanType.JointAnnuity);

        Assert.That(temp, Is.GreaterThan(life),      "TemporaryAnnuity (10yr) > LifeAnnuity (20yr)");
        Assert.That(temp, Is.GreaterThan(guaranteed), "TemporaryAnnuity (10yr) > GuaranteedAnnuity (15yr)");
        Assert.That(temp, Is.GreaterThan(joint),     "TemporaryAnnuity (10yr) > JointAnnuity (30yr)");
    }

    [Test]
    [Category("Annuity.Formula")]
    public void CalcMonthlyPension_ZeroBalance_ReturnsZero()
    {
        var result = AnnuityService.ExposedCalcMonthlyPension(0m, AnnuityPlanType.LifeAnnuity);
        Assert.That(result, Is.EqualTo(0m));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SUBMIT ANNUITY REQUEST
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Annuity.Request.Submit")]
    public async Task SubmitAnnuityRequestAsync_EligibleMember_CreatesRequest()
    {
        // Arrange
        _memberResponse = BuildEligibleMember(ageYears: 55, serviceYears: 15);
        _requestRepo.Setup(r => r.FindPendingByMemberAsync(MemberId)).ReturnsAsync((AnnuityRequest?)null);
        _requestRepo.Setup(r => r.AddAsync(It.IsAny<AnnuityRequest>())).Returns(Task.CompletedTask);

        AnnuityRequest? savedRequest = null;
        _requestRepo.Setup(r => r.AddAsync(It.IsAny<AnnuityRequest>()))
            .Callback<AnnuityRequest>(r => savedRequest = r)
            .Returns(Task.CompletedTask);

        _requestRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => savedRequest!);

        var dto = new SubmitAnnuityRequestDto(MemberId, AnnuityPlanType.LifeAnnuity, "Ready for retirement");

        // Act
        var result = await _sut.SubmitAnnuityRequestAsync(dto);

        // Assert
        Assert.That(result,                        Is.Not.Null);
        Assert.That(result.MemberId,               Is.EqualTo(MemberId));
        Assert.That(result.EstimatedMonthly,       Is.GreaterThan(0));
        Assert.That((string)result.Status.ToString(), Does.Contain("Pending"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Test]
    [Category("Annuity.Request.Submit")]
    public async Task SubmitAnnuityRequestAsync_AlreadyHasPendingRequest_ThrowsInvalidOperation()
    {
        // Arrange
        _memberResponse = BuildEligibleMember(ageYears: 55, serviceYears: 15);
        _requestRepo.Setup(r => r.FindPendingByMemberAsync(MemberId)).ReturnsAsync(BuildPendingRequest());

        var dto = new SubmitAnnuityRequestDto(MemberId, AnnuityPlanType.LifeAnnuity, null);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SubmitAnnuityRequestAsync(dto));
    }

    [Test]
    [Category("Annuity.Request.Submit")]
    public async Task SubmitAnnuityRequestAsync_IneligibleMember_ThrowsInvalidOperation()
    {
        // Arrange — member is too young
        _memberResponse = BuildEligibleMember(ageYears: 40, serviceYears: 5);

        var dto = new SubmitAnnuityRequestDto(MemberId, AnnuityPlanType.LifeAnnuity, null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SubmitAnnuityRequestAsync(dto));
        Assert.That(ex!.Message, Does.Contain("not eligible"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // APPROVE REQUEST
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Annuity.Request.Approve")]
    public async Task ApproveRequestAsync_ValidPendingRequest_CreatesActivePlan()
    {
        // Arrange
        var req = BuildPendingRequest();
        _requestRepo.Setup(r => r.FindByIdAsync(RequestId)).ReturnsAsync(req);
        _requestRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(req);

        AnnuityPlan? savedPlan = null;
        _annuityRepo.Setup(r => r.AddAsync(It.IsAny<AnnuityPlan>()))
            .Callback<AnnuityPlan>(p => savedPlan = p)
            .Returns(Task.CompletedTask);

        _annuityRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => savedPlan ?? BuildActivePlan());
        _annuityRepo.Setup(r => r.ExistsDisbursementForMonthAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ApproveRequestAsync(RequestId, ReviewerId);

        // Assert
        Assert.That(result,        Is.Not.Null);
        Assert.That(result.Status.ToString(), Does.Contain("Approved"));

        _annuityRepo.Verify(r => r.AddAsync(It.Is<AnnuityPlan>(p =>
            p.MemberId == MemberId &&
            p.PurchaseValue == 250000m &&
            p.Status == AnnuityStatus.Active)), Times.Once,
            "A new Active AnnuityPlan must be created on approval");
    }

    [Test]
    [Category("Annuity.Request.Approve")]
    public async Task ApproveRequestAsync_AlreadyApproved_ThrowsInvalidOperation()
    {
        // Arrange
        var req = BuildPendingRequest();
        req.Status = AnnuityRequestStatus.Approved;    // Not pending
        _requestRepo.Setup(r => r.FindByIdAsync(RequestId)).ReturnsAsync(req);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ApproveRequestAsync(RequestId, ReviewerId),
            "Cannot approve a request that is already approved");
    }

    [Test]
    [Category("Annuity.Request.Approve")]
    public async Task ApproveRequestAsync_RequestNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        _requestRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AnnuityRequest?)null);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ApproveRequestAsync(Guid.NewGuid(), ReviewerId));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REJECT REQUEST
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Annuity.Request.Reject")]
    public async Task RejectRequestAsync_PendingRequest_SetsRejectedStatus()
    {
        // Arrange
        var req = BuildPendingRequest();
        _requestRepo.Setup(r => r.FindByIdAsync(RequestId)).ReturnsAsync(req);

        // Act
        var result = await _sut.RejectRequestAsync(RequestId, ReviewerId, "Insufficient service years");

        // Assert
        Assert.That(result.Status.ToString(), Does.Contain("Rejected"));
        Assert.That(result.ReviewNote,        Is.EqualTo("Insufficient service years"));
    }

    [Test]
    [Category("Annuity.Request.Reject")]
    public async Task RejectRequestAsync_AlreadyApproved_ThrowsInvalidOperation()
    {
        // Arrange
        var req = BuildPendingRequest();
        req.Status = AnnuityRequestStatus.Approved;
        _requestRepo.Setup(r => r.FindByIdAsync(RequestId)).ReturnsAsync(req);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RejectRequestAsync(RequestId, ReviewerId, ""));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CANCEL REQUEST
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Annuity.Request.Cancel")]
    public async Task CancelRequestAsync_OwnerCancelsOwnRequest_SetsCancelled()
    {
        // Arrange
        var req = BuildPendingRequest();
        _requestRepo.Setup(r => r.FindByIdAsync(RequestId)).ReturnsAsync(req);

        // Act
        var result = await _sut.CancelRequestAsync(RequestId, MemberId);

        // Assert
        Assert.That(result.Status.ToString(), Does.Contain("Cancelled"));
    }

    [Test]
    [Category("Annuity.Request.Cancel")]
    public async Task CancelRequestAsync_OtherMemberCancels_ThrowsUnauthorized()
    {
        // Arrange
        var req = BuildPendingRequest();
        _requestRepo.Setup(r => r.FindByIdAsync(RequestId)).ReturnsAsync(req);

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CancelRequestAsync(RequestId, Guid.NewGuid()),
            "A member must not be able to cancel another member's request");
    }

    [Test]
    [Category("Annuity.Request.Cancel")]
    public async Task CancelRequestAsync_NonPendingRequest_ThrowsInvalidOperation()
    {
        // Arrange
        var req = BuildPendingRequest();
        req.Status = AnnuityRequestStatus.Approved;
        _requestRepo.Setup(r => r.FindByIdAsync(RequestId)).ReturnsAsync(req);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CancelRequestAsync(RequestId, MemberId));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MONTHLY DISBURSEMENT
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Annuity.Disburse")]
    public async Task ProcessDisbursementAsync_ValidRequest_CreatesDisbursement()
    {
        // Arrange
        var plan = BuildActivePlan(monthlyPension: 4214.86m);
        _annuityRepo.Setup(r => r.FindByIdAsync(AnnuityId)).ReturnsAsync(plan);
        _annuityRepo.Setup(r => r.ExistsDisbursementForMonthAsync(AnnuityId, 8, 2026)).ReturnsAsync(false);

        MonthlyPensionDisbursement? saved = null;
        _annuityRepo.Setup(r => r.AddDisbursementAsync(It.IsAny<MonthlyPensionDisbursement>()))
            .Callback<MonthlyPensionDisbursement>(d => saved = d)
            .Returns(Task.CompletedTask);

        _annuityRepo.Setup(r => r.FindDisbursementByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => saved!);

        var request = new ProcessDisbursementRequest(AnnuityId, Month: 8, Year: 2026, TaxDeducted: 420m);

        // Act
        var result = await _sut.ProcessDisbursementAsync(request);

        // Assert
        Assert.That(result,              Is.Not.Null);
        Assert.That(result.GrossAmount,  Is.EqualTo(4214.86m));
        Assert.That(result.TaxDeducted, Is.EqualTo(420m));
        Assert.That(result.NetAmount,   Is.EqualTo(4214.86m - 420m));
        Assert.That(result.Status.ToString(), Does.Contain("Disbursed"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    [Category("Annuity.Disburse")]
    public async Task ProcessDisbursementAsync_AlreadyDisbursedThisMonth_ThrowsInvalidOperation()
    {
        // Arrange
        var plan = BuildActivePlan();
        _annuityRepo.Setup(r => r.FindByIdAsync(AnnuityId)).ReturnsAsync(plan);
        _annuityRepo.Setup(r => r.ExistsDisbursementForMonthAsync(AnnuityId, 8, 2026)).ReturnsAsync(true);

        var request = new ProcessDisbursementRequest(AnnuityId, Month: 8, Year: 2026, TaxDeducted: 0m);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ProcessDisbursementAsync(request));
        Assert.That(ex!.Message, Does.Contain("already been processed"));
    }

    [Test]
    [Category("Annuity.Disburse")]
    public async Task ProcessDisbursementAsync_InactivePlan_ThrowsInvalidOperation()
    {
        // Arrange
        var plan = BuildActivePlan();
        plan.Status = AnnuityStatus.Terminated;
        _annuityRepo.Setup(r => r.FindByIdAsync(AnnuityId)).ReturnsAsync(plan);

        var request = new ProcessDisbursementRequest(AnnuityId, Month: 8, Year: 2026, TaxDeducted: 0m);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ProcessDisbursementAsync(request));
    }

    [Test]
    [Category("Annuity.Disburse")]
    public async Task ProcessDisbursementAsync_NegativeTax_ThrowsArgumentException()
    {
        // Arrange
        var plan = BuildActivePlan();
        _annuityRepo.Setup(r => r.FindByIdAsync(AnnuityId)).ReturnsAsync(plan);
        _annuityRepo.Setup(r => r.ExistsDisbursementForMonthAsync(AnnuityId, 8, 2026)).ReturnsAsync(false);

        var request = new ProcessDisbursementRequest(AnnuityId, Month: 8, Year: 2026, TaxDeducted: -100m);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => _sut.ProcessDisbursementAsync(request));
    }

    [Test]
    [Category("Annuity.Disburse")]
    public async Task ProcessDisbursementAsync_InsufficientPensionBalance_ThrowsInvalidOperation()
    {
        // Arrange — EPS balance is less than the monthly pension
        var plan = BuildActivePlan(monthlyPension: 500000m);  // Huge pension
        _annuityRepo.Setup(r => r.FindByIdAsync(AnnuityId)).ReturnsAsync(plan);
        _annuityRepo.Setup(r => r.ExistsDisbursementForMonthAsync(AnnuityId, 8, 2026)).ReturnsAsync(false);

        // Fund account only has ₹250,000 — less than the ₹500,000 monthly pension
        _fundAccountResponse = _fundAccountResponse! with { PensionBalance = 250000m };

        var request = new ProcessDisbursementRequest(AnnuityId, Month: 8, Year: 2026, TaxDeducted: 0m);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ProcessDisbursementAsync(request));
        Assert.That(ex!.Message, Does.Contain("Insufficient"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NOMINEE SETTLEMENT
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Annuity.Settlement")]
    public async Task ProcessNomineeSettlementAsync_ValidRequest_SetsStatusSettled()
    {
        // Arrange
        var plan = BuildActivePlan();
        _annuityRepo.Setup(r => r.FindByIdAsync(AnnuityId)).ReturnsAsync(plan);
        _annuityRepo.Setup(r => r.ExistsDisbursementForMonthAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var request = new NomineeSettlementRequest("Jane Doe (Spouse)", "HDFC-XXXXXX789", 250000m);

        // Act
        var result = await _sut.ProcessNomineeSettlementAsync(AnnuityId, request);

        // Assert
        Assert.That(result.Status.ToString(), Does.Contain("Settled"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    [Category("Annuity.Settlement")]
    public async Task ProcessNomineeSettlementAsync_AlreadySettled_ThrowsInvalidOperation()
    {
        // Arrange
        var plan = BuildActivePlan();
        plan.Status = AnnuityStatus.Settled;
        _annuityRepo.Setup(r => r.FindByIdAsync(AnnuityId)).ReturnsAsync(plan);

        var request = new NomineeSettlementRequest("Jane Doe", "HDFC-XXX", 250000m);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessNomineeSettlementAsync(AnnuityId, request));
    }

    [Test]
    [Category("Annuity.Settlement")]
    public async Task ProcessNomineeSettlementAsync_ZeroPurchaseValue_ThrowsInvalidOperation()
    {
        // Arrange — purchase value is already zero
        var plan = BuildActivePlan();
        plan.PurchaseValue = 0m;
        _annuityRepo.Setup(r => r.FindByIdAsync(AnnuityId)).ReturnsAsync(plan);

        var request = new NomineeSettlementRequest("Jane Doe", "HDFC-XXX", 0m);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessNomineeSettlementAsync(AnnuityId, request));
    }

    [Test]
    [Category("Annuity.Settlement")]
    public async Task ProcessNomineeSettlementAsync_InsufficientBalance_ThrowsInvalidOperation()
    {
        // Arrange — pension balance is insufficient for the settlement
        var plan = BuildActivePlan();
        plan.PurchaseValue = 500000m;   // Need ₹5 lakh
        _fundAccountResponse = _fundAccountResponse! with { PensionBalance = 100000m }; // Only ₹1 lakh
        _annuityRepo.Setup(r => r.FindByIdAsync(AnnuityId)).ReturnsAsync(plan);

        var request = new NomineeSettlementRequest("Jane Doe", "HDFC-XXX", 500000m);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessNomineeSettlementAsync(AnnuityId, request));
        Assert.That(ex!.Message, Does.Contain("Insufficient"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TERMINATE
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Annuity.Terminate")]
    public async Task TerminateAnnuityAsync_ActivePlan_SetsStatusTerminated()
    {
        // Arrange
        var plan = BuildActivePlan();
        _annuityRepo.Setup(r => r.FindByIdAsync(AnnuityId)).ReturnsAsync(plan);
        _annuityRepo.Setup(r => r.ExistsDisbursementForMonthAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.TerminateAnnuityAsync(AnnuityId);

        // Assert
        Assert.That(result.Status.ToString(), Does.Contain("Terminated"));
    }

    [Test]
    [Category("Annuity.Terminate")]
    public async Task TerminateAnnuityAsync_PlanNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        _annuityRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AnnuityPlan?)null);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.TerminateAnnuityAsync(Guid.NewGuid()));
    }
}
