using System.Net;
using Annuity.Domain.Entities;
using Annuity.Domain.Repositories;
using Annuity.Services;
using Annuity.Services.DTOs;
using Annuity.Services.HttpClients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PensionVault.Shared.Contracts;

namespace Annuity_test;

/// <summary>
/// Unit tests for <see cref="AnnuityService"/> business logic. Repository and unit-of-work
/// dependencies are mocked with Moq; the concrete HTTP clients are driven through a
/// <see cref="StubHttpMessageHandler"/> so no live services are required.
/// </summary>
[TestFixture]
public class AnnuityServiceTests
{
    private static (AnnuityService svc,
                    Mock<IAnnuityRepository> annuityRepo,
                    Mock<IAnnuityRequestRepository> requestRepo,
                    Mock<IUnitOfWork> uow)
        Build(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        responder ??= _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var http = new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var ctx = Mock.Of<IHttpContextAccessor>();

        var memberClient = new MemberServiceClient(http, ctx);
        var contribClient = new ContributionsServiceClient(http, ctx);
        var notifClient = new NotificationServiceClient(http, ctx, Mock.Of<ILogger<NotificationServiceClient>>());

        var annuityRepo = new Mock<IAnnuityRepository>();
        var requestRepo = new Mock<IAnnuityRequestRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var svc = new AnnuityService(annuityRepo.Object, requestRepo.Object,
            memberClient, contribClient, notifClient, uow.Object);

        return (svc, annuityRepo, requestRepo, uow);
    }

    /// <summary>Routes canned JSON responses by request path for the three downstream services.</summary>
    private static Func<HttpRequestMessage, HttpResponseMessage> Backend(
        MemberResponse? member = null,
        FundAccountResponse? account = null,
        object[]? contributions = null)
        => req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.Contains("/api/members/"))
                return member is null ? TestSupport.NotFound() : TestSupport.Json(member);
            if (path.Contains("/api/remittances/member/"))
                return TestSupport.Json(contributions ?? Array.Empty<object>());
            if (path.Contains("/api/fundaccounts/active/member/"))
                return account is null ? TestSupport.NotFound() : TestSupport.Json(account);
            if (path.Contains("/api/ledger") || path.Contains("/api/notifications"))
                return TestSupport.Ok();
            return TestSupport.NotFound();
        };

    // ---------- Eligibility ----------

    [Test]
    public async Task CheckEligibility_AllCriteriaMet_ReturnsEligible()
    {
        var memberId = Guid.NewGuid();
        var member = TestSupport.Member(memberId, dob: DateTime.UtcNow.AddYears(-55),
            joining: DateTime.UtcNow.AddYears(-15), status: "Active");
        var account = TestSupport.Account(memberId, pensionBalance: 100000m);
        var contributions = new object[] { new { Period = "2020-01" }, new { Period = "2020-02" } };

        var (svc, _, _, _) = Build(Backend(member, account, contributions));

        var result = await svc.CheckEligibilityAsync(memberId);

        Assert.That(result.IsEligible, Is.True);
        Assert.That(result.AgeYears, Is.EqualTo(55));
        Assert.That(result.ServiceYears, Is.EqualTo(15));
        Assert.That(result.ContributionMonths, Is.EqualTo(2));
        Assert.That(result.PensionBalance, Is.EqualTo(100000m));
        Assert.That(result.FailureReasons, Is.Empty);
    }

    [Test]
    public async Task CheckEligibility_TooYoung_ReturnsIneligible()
    {
        var memberId = Guid.NewGuid();
        var member = TestSupport.Member(memberId, dob: DateTime.UtcNow.AddYears(-40),
            joining: DateTime.UtcNow.AddYears(-15), status: "Active");
        var account = TestSupport.Account(memberId, pensionBalance: 100000m);

        var (svc, _, _, _) = Build(Backend(member, account));

        var result = await svc.CheckEligibilityAsync(memberId);

        Assert.That(result.IsEligible, Is.False);
        Assert.That(result.FailureReasons, Has.Some.Contains("Age must be at least"));
    }

    [Test]
    public void CheckEligibility_MemberNotFound_ThrowsKeyNotFound()
    {
        var (svc, _, _, _) = Build(Backend(member: null));
        Assert.ThrowsAsync<KeyNotFoundException>(() => svc.CheckEligibilityAsync(Guid.NewGuid()));
    }

    // ---------- CreateAnnuity validation ----------

    [Test]
    public void CreateAnnuity_PurchaseValueNotPositive_ThrowsArgumentException()
    {
        var (svc, _, _, _) = Build();
        var req = new CreateAnnuityRequest(Guid.NewGuid(), AnnuityPlanType.LifeAnnuity,
            PurchaseValue: 0m, MonthlyPension: 100m, AnnuityStartDate: DateTime.UtcNow,
            NomineeName: "N", NomineeRelation: "Spouse", NomineeBankAccount: "ACC", NomineePercent: 100);

        Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAnnuityAsync(req));
    }

    [Test]
    public void CreateAnnuity_MonthlyPensionNotPositive_ThrowsArgumentException()
    {
        var (svc, _, _, _) = Build();
        var req = new CreateAnnuityRequest(Guid.NewGuid(), AnnuityPlanType.LifeAnnuity,
            PurchaseValue: 100000m, MonthlyPension: 0m, AnnuityStartDate: DateTime.UtcNow,
            NomineeName: "N", NomineeRelation: "Spouse", NomineeBankAccount: "ACC", NomineePercent: 100);

        Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAnnuityAsync(req));
    }

    // ---------- Request lifecycle guards ----------

    [Test]
    public void CancelRequest_NotFound_ThrowsKeyNotFound()
    {
        var (svc, _, requestRepo, _) = Build();
        requestRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AnnuityRequest?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() => svc.CancelRequestAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public void CancelRequest_NotOwner_ThrowsUnauthorized()
    {
        var (svc, _, requestRepo, _) = Build();
        var request = new AnnuityRequest { MemberId = Guid.NewGuid(), Status = AnnuityRequestStatus.Pending };
        requestRepo.Setup(r => r.FindByIdAsync(request.RequestId)).ReturnsAsync(request);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.CancelRequestAsync(request.RequestId, Guid.NewGuid()));
    }

    [Test]
    public void CancelRequest_NotPending_ThrowsInvalidOperation()
    {
        var (svc, _, requestRepo, _) = Build();
        var memberId = Guid.NewGuid();
        var request = new AnnuityRequest { MemberId = memberId, Status = AnnuityRequestStatus.Approved };
        requestRepo.Setup(r => r.FindByIdAsync(request.RequestId)).ReturnsAsync(request);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelRequestAsync(request.RequestId, memberId));
    }

    [Test]
    public async Task CancelRequest_Valid_SetsCancelledAndSaves()
    {
        var memberId = Guid.NewGuid();
        var member = TestSupport.Member(memberId);
        var (svc, _, requestRepo, uow) = Build(Backend(member));
        var request = new AnnuityRequest { MemberId = memberId, Status = AnnuityRequestStatus.Pending };
        requestRepo.Setup(r => r.FindByIdAsync(request.RequestId)).ReturnsAsync(request);

        var result = await svc.CancelRequestAsync(request.RequestId, memberId);

        Assert.That(request.Status, Is.EqualTo(AnnuityRequestStatus.Cancelled));
        Assert.That(result.Status, Is.EqualTo(AnnuityRequestStatus.Cancelled));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ApproveRequest_NotPending_ThrowsInvalidOperation()
    {
        var (svc, _, requestRepo, _) = Build();
        var request = new AnnuityRequest { MemberId = Guid.NewGuid(), Status = AnnuityRequestStatus.Rejected };
        requestRepo.Setup(r => r.FindByIdAsync(request.RequestId)).ReturnsAsync(request);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ApproveRequestAsync(request.RequestId, Guid.NewGuid()));
    }

    [Test]
    public void RejectRequest_NotPending_ThrowsInvalidOperation()
    {
        var (svc, _, requestRepo, _) = Build();
        var request = new AnnuityRequest { MemberId = Guid.NewGuid(), Status = AnnuityRequestStatus.Approved };
        requestRepo.Setup(r => r.FindByIdAsync(request.RequestId)).ReturnsAsync(request);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RejectRequestAsync(request.RequestId, Guid.NewGuid(), "no"));
    }

    [Test]
    public void SubmitAnnuityRequest_Ineligible_ThrowsInvalidOperation()
    {
        var memberId = Guid.NewGuid();
        // 40 years old -> fails the minimum age rule -> ineligible.
        var member = TestSupport.Member(memberId, dob: DateTime.UtcNow.AddYears(-40));
        var account = TestSupport.Account(memberId, pensionBalance: 0m);
        var (svc, _, _, _) = Build(Backend(member, account));

        var dto = new SubmitAnnuityRequestDto(memberId, AnnuityPlanType.LifeAnnuity, "note");
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.SubmitAnnuityRequestAsync(dto));
    }

    // ---------- Disbursement guards ----------

    [Test]
    public void ProcessDisbursement_AnnuityNotActive_ThrowsInvalidOperation()
    {
        var (svc, annuityRepo, _, _) = Build();
        var plan = new AnnuityPlan { Status = AnnuityStatus.Terminated };
        annuityRepo.Setup(r => r.FindByIdAsync(plan.AnnuityId)).ReturnsAsync(plan);

        var req = new ProcessDisbursementRequest(plan.AnnuityId, 5, 2026, 0m);
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.ProcessDisbursementAsync(req));
    }

    [Test]
    public void ProcessDisbursement_AlreadyDisbursed_ThrowsInvalidOperation()
    {
        var (svc, annuityRepo, _, _) = Build();
        var plan = new AnnuityPlan { Status = AnnuityStatus.Active, MonthlyPension = 1000m };
        annuityRepo.Setup(r => r.FindByIdAsync(plan.AnnuityId)).ReturnsAsync(plan);
        annuityRepo.Setup(r => r.ExistsDisbursementForMonthAsync(plan.AnnuityId, 5, 2026)).ReturnsAsync(true);

        var req = new ProcessDisbursementRequest(plan.AnnuityId, 5, 2026, 0m);
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.ProcessDisbursementAsync(req));
    }

    [Test]
    public void ProcessDisbursement_NegativeTax_ThrowsArgumentException()
    {
        var (svc, annuityRepo, _, _) = Build();
        var plan = new AnnuityPlan { Status = AnnuityStatus.Active, MonthlyPension = 1000m };
        annuityRepo.Setup(r => r.FindByIdAsync(plan.AnnuityId)).ReturnsAsync(plan);
        annuityRepo.Setup(r => r.ExistsDisbursementForMonthAsync(plan.AnnuityId, 5, 2026)).ReturnsAsync(false);

        var req = new ProcessDisbursementRequest(plan.AnnuityId, 5, 2026, -1m);
        Assert.ThrowsAsync<ArgumentException>(() => svc.ProcessDisbursementAsync(req));
    }

    // ---------- Terminate ----------

    [Test]
    public async Task TerminateAnnuity_SetsStatusTerminatedAndSaves()
    {
        var memberId = Guid.NewGuid();
        var member = TestSupport.Member(memberId);
        var (svc, annuityRepo, _, uow) = Build(Backend(member));
        var plan = new AnnuityPlan { MemberId = memberId, Status = AnnuityStatus.Active };
        annuityRepo.Setup(r => r.FindByIdAsync(plan.AnnuityId)).ReturnsAsync(plan);
        annuityRepo.Setup(r => r.ExistsDisbursementForMonthAsync(plan.AnnuityId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var result = await svc.TerminateAnnuityAsync(plan.AnnuityId);

        Assert.That(plan.Status, Is.EqualTo(AnnuityStatus.Terminated));
        Assert.That(result.Status, Is.EqualTo(AnnuityStatus.Terminated));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
