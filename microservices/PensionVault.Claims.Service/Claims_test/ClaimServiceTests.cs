using System.Net;
using Claims.Domain.Entities;
using Claims.Domain.Repositories;
using Claims.Services;
using Claims.Services.DTOs;
using Claims.Services.HttpClients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PensionVault.Shared.HttpClients;

namespace Claims_test;

/// <summary>
/// Unit tests for <see cref="ClaimService"/> business logic. The claim repository and
/// unit-of-work are mocked with Moq; the concrete HTTP clients (Members, Contributions,
/// Notification, Audit) are driven through a <see cref="StubHttpMessageHandler"/>.
/// </summary>
[TestFixture]
public class ClaimServiceTests
{
    private const string ValidDescription = "A valid claim description that is long enough.";

    private static (ClaimService svc, Mock<IClaimRepository> repo, Mock<IUnitOfWork> uow)
        Build(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        responder ??= _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var http = new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var ctx = Mock.Of<IHttpContextAccessor>();

        var memberClient = new MembersServiceClient(http, ctx);
        var contribClient = new ContributionsServiceClient(http, ctx);
        var notifClient = new NotificationServiceClient(http, ctx, Mock.Of<ILogger<NotificationServiceClient>>());
        var auditClient = new AuditServiceClient(http, Mock.Of<ILogger<AuditServiceClient>>());

        var repo = new Mock<IClaimRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var svc = new ClaimService(repo.Object, memberClient, contribClient, notifClient, auditClient, uow.Object);
        return (svc, repo, uow);
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> Backend(
        PensionVault.Shared.Contracts.MemberResponse? member = null,
        PensionVault.Shared.Contracts.FundAccountResponse? account = null)
        => req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.Contains("/api/users/by-role/"))
                return TestSupport.Json(Array.Empty<object>());
            if (path.Contains("/api/members/"))
                return member is null ? TestSupport.NotFound() : TestSupport.Json(member);
            if (path.Contains("/api/fundaccounts/active/member/"))
                return account is null ? TestSupport.NotFound() : TestSupport.Json(account);
            if (path.Contains("/api/ledger") || path.Contains("/api/notifications") || path.Contains("/api/audit"))
                return TestSupport.Ok();
            return TestSupport.NotFound();
        };

    // ---------- SubmitClaim validation ----------

    [Test]
    public void SubmitClaim_EmptyMemberId_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        var req = new CreateClaimRequest(Guid.Empty, ClaimType.Retirement, 1000m, "Retirement", ValidDescription);
        Assert.ThrowsAsync<ArgumentException>(() => svc.SubmitClaimAsync(req));
    }

    [Test]
    public void SubmitClaim_NonPositiveAmount_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        var req = new CreateClaimRequest(Guid.NewGuid(), ClaimType.Retirement, 0m, "Retirement", ValidDescription);
        Assert.ThrowsAsync<ArgumentException>(() => svc.SubmitClaimAsync(req));
    }

    [Test]
    public void SubmitClaim_PartialWithdrawalType_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        var req = new CreateClaimRequest(Guid.NewGuid(), ClaimType.PartialWithdrawal, 1000m, "x", ValidDescription);
        Assert.ThrowsAsync<ArgumentException>(() => svc.SubmitClaimAsync(req));
    }

    [Test]
    public void SubmitClaim_ShortDescription_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        var req = new CreateClaimRequest(Guid.NewGuid(), ClaimType.Retirement, 1000m, "Retirement", "too short");
        Assert.ThrowsAsync<ArgumentException>(() => svc.SubmitClaimAsync(req));
    }

    [Test]
    public void SubmitClaim_MemberNotFound_ThrowsKeyNotFound()
    {
        var (svc, _, _) = Build(Backend(member: null));
        var req = new CreateClaimRequest(Guid.NewGuid(), ClaimType.Retirement, 1000m, "Retirement", ValidDescription);
        Assert.ThrowsAsync<KeyNotFoundException>(() => svc.SubmitClaimAsync(req));
    }

    [Test]
    public void SubmitClaim_NoActiveAccount_ThrowsInvalidOperation()
    {
        var memberId = Guid.NewGuid();
        var (svc, _, _) = Build(Backend(TestSupport.Member(memberId), account: null));
        var req = new CreateClaimRequest(memberId, ClaimType.Retirement, 1000m, "Retirement", ValidDescription);
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.SubmitClaimAsync(req));
    }

    [Test]
    public void SubmitClaim_InsufficientEpf_ThrowsInvalidOperation()
    {
        var memberId = Guid.NewGuid();
        var member = TestSupport.Member(memberId);
        var account = TestSupport.Account(memberId, totalBalance: 1000m);
        var (svc, repo, _) = Build(Backend(member, account));
        // Reserve almost the whole balance so the new claim exceeds what's available.
        repo.Setup(r => r.GetActiveClaimsTotalAsync(memberId)).ReturnsAsync(900m);

        var req = new CreateClaimRequest(memberId, ClaimType.Retirement, 500m, "Retirement", ValidDescription);
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.SubmitClaimAsync(req));
    }

    [Test]
    public async Task SubmitClaim_Valid_CreatesClaimAndReturnsResponse()
    {
        var memberId = Guid.NewGuid();
        var member = TestSupport.Member(memberId);
        var account = TestSupport.Account(memberId, totalBalance: 200000m, vestingPercent: 100m);
        var (svc, repo, uow) = Build(Backend(member, account));

        BenefitClaim? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<BenefitClaim>()))
            .Callback<BenefitClaim>(c => captured = c)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.GetActiveClaimsTotalAsync(memberId)).ReturnsAsync(0m);
        repo.Setup(r => r.HasRecentDuplicateAsync(It.IsAny<Guid>(), It.IsAny<ClaimType>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(() => captured);

        var req = new CreateClaimRequest(memberId, ClaimType.Retirement, 1000m, "Retirement", ValidDescription);
        var result = await svc.SubmitClaimAsync(req);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Status, Is.EqualTo(ClaimStatus.Submitted));
        Assert.That(captured.VestedAmount, Is.EqualTo(200000m));   // total * vesting%
        Assert.That(captured.TaxDeductible, Is.EqualTo(100m));     // 10% of 1000
        Assert.That(result.Status, Is.EqualTo("Submitted"));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void SubmitClaim_DuplicateWithinWindow_ThrowsInvalidOperation()
    {
        var memberId = Guid.NewGuid();
        var member = TestSupport.Member(memberId);
        var account = TestSupport.Account(memberId, totalBalance: 200000m);
        var (svc, repo, _) = Build(Backend(member, account));
        repo.Setup(r => r.GetActiveClaimsTotalAsync(memberId)).ReturnsAsync(0m);
        repo.Setup(r => r.HasRecentDuplicateAsync(It.IsAny<Guid>(), It.IsAny<ClaimType>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        var req = new CreateClaimRequest(memberId, ClaimType.Retirement, 1000m, "Retirement", ValidDescription);
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.SubmitClaimAsync(req));
    }

    // ---------- Disburse validation ----------

    [Test]
    public void Disburse_NonPositiveAmount_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        Assert.ThrowsAsync<ArgumentException>(
            () => svc.DisburseClaimAsync(Guid.NewGuid(), new DisburseClaimRequest(0m, 0m, "ACC")));
    }

    [Test]
    public void Disburse_NegativeTax_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        Assert.ThrowsAsync<ArgumentException>(
            () => svc.DisburseClaimAsync(Guid.NewGuid(), new DisburseClaimRequest(1000m, -1m, "ACC")));
    }

    [Test]
    public void Disburse_ClaimNotFound_ThrowsKeyNotFound()
    {
        var (svc, repo, _) = Build();
        repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BenefitClaim?)null);
        Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.DisburseClaimAsync(Guid.NewGuid(), new DisburseClaimRequest(1000m, 0m, "ACC")));
    }

    [Test]
    public void Disburse_NotApproved_ThrowsInvalidOperation()
    {
        var (svc, repo, _) = Build();
        var claim = new BenefitClaim { Status = ClaimStatus.Submitted };
        repo.Setup(r => r.FindByIdAsync(claim.ClaimId)).ReturnsAsync(claim);
        Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DisburseClaimAsync(claim.ClaimId, new DisburseClaimRequest(1000m, 0m, "ACC")));
    }

    [Test]
    public void Disburse_AlreadyDisbursed_ThrowsInvalidOperation()
    {
        var (svc, repo, _) = Build();
        var claim = new BenefitClaim { Status = ClaimStatus.Disbursed };
        repo.Setup(r => r.FindByIdAsync(claim.ClaimId)).ReturnsAsync(claim);
        Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DisburseClaimAsync(claim.ClaimId, new DisburseClaimRequest(1000m, 0m, "ACC")));
    }

    [Test]
    public void Disburse_NetNotPositive_ThrowsArgument()
    {
        var (svc, repo, _) = Build();
        var claim = new BenefitClaim { Status = ClaimStatus.Approved };
        repo.Setup(r => r.FindByIdAsync(claim.ClaimId)).ReturnsAsync(claim);
        // disbursed == tax => net == 0
        Assert.ThrowsAsync<ArgumentException>(
            () => svc.DisburseClaimAsync(claim.ClaimId, new DisburseClaimRequest(1000m, 1000m, "ACC")));
    }

    [Test]
    public void Disburse_InsufficientBalance_ThrowsInvalidOperation()
    {
        var memberId = Guid.NewGuid();
        var account = TestSupport.Account(memberId, totalBalance: 100m);
        var (svc, repo, _) = Build(Backend(TestSupport.Member(memberId), account));
        var claim = new BenefitClaim { MemberId = memberId, Status = ClaimStatus.Approved };
        repo.Setup(r => r.FindByIdAsync(claim.ClaimId)).ReturnsAsync(claim);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DisburseClaimAsync(claim.ClaimId, new DisburseClaimRequest(1000m, 0m, "ACC")));
    }

    // ---------- Status transitions ----------

    [Test]
    public void ApproveClaim_AlreadyInTargetStatus_ThrowsInvalidOperation()
    {
        var (svc, repo, _) = Build();
        var claim = new BenefitClaim { Status = ClaimStatus.Approved };
        repo.Setup(r => r.FindByIdAsync(claim.ClaimId)).ReturnsAsync(claim);
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.ApproveClaimAsync(claim.ClaimId, Guid.NewGuid()));
    }

    [Test]
    public void ReviewClaim_WhenDisbursed_ThrowsInvalidOperation()
    {
        var (svc, repo, _) = Build();
        var claim = new BenefitClaim { Status = ClaimStatus.Disbursed };
        repo.Setup(r => r.FindByIdAsync(claim.ClaimId)).ReturnsAsync(claim);
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.ReviewClaimAsync(claim.ClaimId, Guid.NewGuid()));
    }

    [Test]
    public async Task ApproveClaim_Valid_SetsApprovedAndSaves()
    {
        var memberId = Guid.NewGuid();
        var (svc, repo, uow) = Build(Backend(TestSupport.Member(memberId)));
        var claim = new BenefitClaim { MemberId = memberId, Status = ClaimStatus.Submitted };
        repo.Setup(r => r.FindByIdAsync(claim.ClaimId)).ReturnsAsync(claim);

        var processedBy = Guid.NewGuid();
        var result = await svc.ApproveClaimAsync(claim.ClaimId, processedBy);

        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Approved));
        Assert.That(claim.ProcessedById, Is.EqualTo(processedBy));
        Assert.That(result.Status, Is.EqualTo("Approved"));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Partial withdrawal ----------

    [Test]
    public void SubmitPartialWithdrawal_BlankReason_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        var req = new CreatePartialWithdrawalRequest(Guid.NewGuid(), 1000m, "", ValidDescription);
        Assert.ThrowsAsync<ArgumentException>(() => svc.SubmitPartialWithdrawalAsync(req));
    }

    [Test]
    public void SubmitPartialWithdrawal_ShortDescription_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        var req = new CreatePartialWithdrawalRequest(Guid.NewGuid(), 1000m, "Medical", "short");
        Assert.ThrowsAsync<ArgumentException>(() => svc.SubmitPartialWithdrawalAsync(req));
    }

    [Test]
    public void SubmitPartialWithdrawal_NonPositiveAmount_ThrowsArgument()
    {
        var (svc, _, _) = Build();
        var req = new CreatePartialWithdrawalRequest(Guid.NewGuid(), 0m, "Medical", ValidDescription);
        Assert.ThrowsAsync<ArgumentException>(() => svc.SubmitPartialWithdrawalAsync(req));
    }
}
