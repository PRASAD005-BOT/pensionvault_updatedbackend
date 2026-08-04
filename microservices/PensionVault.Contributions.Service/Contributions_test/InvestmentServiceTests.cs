using System.Net;
using Contributions.Domain.Entities;
using Contributions.Domain.Repositories;
using Contributions.Services;
using Contributions.Services.DTOs;
using Contributions.Services.HttpClients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PensionVault.Shared.Contracts;

namespace Contributions_test;

/// <summary>
/// Unit tests for <see cref="InvestmentService"/> portfolio and corpus rules. The repository
/// is mocked; the Member/Notification HTTP clients are driven through a stub handler.
/// </summary>
[TestFixture]
public class InvestmentServiceTests
{
    private static (InvestmentService svc, Mock<IInvestmentRepository> repo, Mock<IUnitOfWork> uow)
        Build(SchemeResponse? scheme = null)
    {
        Func<HttpRequestMessage, HttpResponseMessage> responder = req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.Contains("/api/schemes/"))
                return scheme is null ? TestSupport.NotFound() : TestSupport.Json(scheme);
            if (path.Contains("/api/users/by-role/"))
                return TestSupport.Json(Array.Empty<object>());
            if (path.Contains("/api/notifications"))
                return TestSupport.Ok();
            return TestSupport.NotFound();
        };

        var http = new HttpClient(new StubHttpMessageHandler(responder)) { BaseAddress = new Uri("http://localhost/") };
        var ctx = Mock.Of<IHttpContextAccessor>();
        var memberClient = new MemberServiceClient(http, ctx);
        var notifClient = new NotificationServiceClient(http, ctx, Mock.Of<ILogger<NotificationServiceClient>>());

        var repo = new Mock<IInvestmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var svc = new InvestmentService(repo.Object, uow.Object, memberClient, notifClient);
        return (svc, repo, uow);
    }

    // ---------- Portfolios ----------

    [Test]
    public void CreatePortfolio_UnknownScheme_ThrowsArgument()
    {
        var (svc, repo, _) = Build(scheme: null);   // scheme lookup returns 404
        var schemeId = Guid.NewGuid();
        repo.Setup(r => r.SchemeExistsAsync(schemeId)).ReturnsAsync(false);

        var req = new CreatePortfolioRequest(schemeId, AssetClass.Equity, 10m, 1000m, 1200m, 0m);
        Assert.ThrowsAsync<ArgumentException>(() => svc.CreatePortfolioAsync(req));
    }

    [Test]
    public void CreatePortfolio_ExceedsAllocation_ThrowsInvalidOperation()
    {
        var schemeId = Guid.NewGuid();
        var (svc, repo, _) = Build(TestSupport.Scheme(schemeId));
        repo.Setup(r => r.SchemeExistsAsync(schemeId)).ReturnsAsync(true);
        repo.Setup(r => r.GetPortfoliosAsync(schemeId)).ReturnsAsync(new List<InvestmentPortfolio>
        {
            new() { SchemeId = schemeId, AllocationPercent = 90m }
        });

        var req = new CreatePortfolioRequest(schemeId, AssetClass.Equity, 20m, 1000m, 1200m, 0m);
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreatePortfolioAsync(req));
    }

    [Test]
    public async Task CreatePortfolio_Valid_ComputesYieldAndReturns()
    {
        var schemeId = Guid.NewGuid();
        var (svc, repo, uow) = Build(TestSupport.Scheme(schemeId, "Equity Growth"));
        repo.Setup(r => r.SchemeExistsAsync(schemeId)).ReturnsAsync(true);
        repo.Setup(r => r.GetPortfoliosAsync(schemeId)).ReturnsAsync(new List<InvestmentPortfolio>());

        InvestmentPortfolio? captured = null;
        repo.Setup(r => r.AddPortfolioAsync(It.IsAny<InvestmentPortfolio>()))
            .Callback<InvestmentPortfolio>(p => captured = p).Returns(Task.CompletedTask);
        repo.Setup(r => r.FindPortfolioByIdAsync(It.IsAny<Guid>())).ReturnsAsync(() => captured);

        var req = new CreatePortfolioRequest(schemeId, AssetClass.Equity, 25m, 1000m, 1250m, 0m);
        var result = await svc.CreatePortfolioAsync(req);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.YieldEarned, Is.EqualTo(250m));    // 1250 - 1000
        Assert.That(result.SchemeName, Is.EqualTo("Equity Growth"));
        Assert.That(result.AllocationPercent, Is.EqualTo(25m));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdatePortfolio_NotFound_ThrowsKeyNotFound()
    {
        var (svc, repo, _) = Build();
        repo.Setup(r => r.FindPortfolioByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InvestmentPortfolio?)null);
        Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.UpdatePortfolioAsync(Guid.NewGuid(), new UpdatePortfolioRequest(10m, 1000m, 1100m, 0m)));
    }

    // ---------- Corpus ----------

    [Test]
    public void CreateCorpus_NegativeClosing_ThrowsInvalidOperation()
    {
        var schemeId = Guid.NewGuid();
        var (svc, repo, _) = Build(TestSupport.Scheme(schemeId));
        repo.Setup(r => r.SchemeExistsAsync(schemeId)).ReturnsAsync(true);
        repo.Setup(r => r.GetLastFinalisedCorpusAsync(schemeId)).ReturnsAsync((CorpusRecord?)null);

        // opening 0 + contributions 0 - withdrawals 100 => -100
        var req = new CreateCorpusRequest(schemeId, DateTime.UtcNow, 0m, 100m, 0m, 0m);
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateCorpusRecordAsync(req));
    }

    [Test]
    public async Task CreateCorpus_Valid_RollsForwardFromLastFinalised()
    {
        var schemeId = Guid.NewGuid();
        var (svc, repo, uow) = Build(TestSupport.Scheme(schemeId));
        repo.Setup(r => r.SchemeExistsAsync(schemeId)).ReturnsAsync(true);
        repo.Setup(r => r.GetLastFinalisedCorpusAsync(schemeId))
            .ReturnsAsync(new CorpusRecord { SchemeId = schemeId, ClosingCorpus = 100000m, Status = CorpusStatus.Finalised });

        CorpusRecord? captured = null;
        repo.Setup(r => r.AddCorpusAsync(It.IsAny<CorpusRecord>()))
            .Callback<CorpusRecord>(c => captured = c).Returns(Task.CompletedTask);
        repo.Setup(r => r.FindCorpusByIdAsync(It.IsAny<Guid>())).ReturnsAsync(() => captured);

        // 100000 + 5000 - 2000 + 1000 - 500 = 103500
        var req = new CreateCorpusRequest(schemeId, DateTime.UtcNow, 5000m, 2000m, 1000m, 500m);
        var result = await svc.CreateCorpusRecordAsync(req);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ClosingCorpus, Is.EqualTo(103500m));
        Assert.That(captured.Status, Is.EqualTo(CorpusStatus.Draft));
        Assert.That(result.ClosingCorpus, Is.EqualTo(103500m));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void FinaliseCorpus_NotFound_ThrowsKeyNotFound()
    {
        var (svc, repo, _) = Build();
        repo.Setup(r => r.FindCorpusByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CorpusRecord?)null);
        Assert.ThrowsAsync<KeyNotFoundException>(() => svc.FinaliseCorpusAsync(Guid.NewGuid()));
    }

    [Test]
    public void FinaliseCorpus_AlreadyFinalised_ThrowsInvalidOperation()
    {
        var (svc, repo, _) = Build();
        var corpus = new CorpusRecord { Status = CorpusStatus.Finalised };
        repo.Setup(r => r.FindCorpusByIdAsync(corpus.CorpusId)).ReturnsAsync(corpus);
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.FinaliseCorpusAsync(corpus.CorpusId));
    }

    [Test]
    public async Task FinaliseCorpus_Valid_SetsFinalised()
    {
        var schemeId = Guid.NewGuid();
        var (svc, repo, uow) = Build(TestSupport.Scheme(schemeId));
        var corpus = new CorpusRecord { SchemeId = schemeId, Status = CorpusStatus.Draft, ClosingCorpus = 50000m };
        repo.Setup(r => r.FindCorpusByIdAsync(corpus.CorpusId)).ReturnsAsync(corpus);

        var result = await svc.FinaliseCorpusAsync(corpus.CorpusId);

        Assert.That(corpus.Status, Is.EqualTo(CorpusStatus.Finalised));
        Assert.That(result.Status, Is.EqualTo(CorpusStatus.Finalised));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
