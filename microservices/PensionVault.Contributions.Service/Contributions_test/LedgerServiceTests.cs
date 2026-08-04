using System.Net;
using Contributions.Domain.Entities;
using Contributions.Domain.Repositories;
using Contributions.Services;
using Contributions.Services.DTOs;
using Contributions.Services.HttpClients;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace Contributions_test;

/// <summary>
/// Unit tests for <see cref="LedgerService"/>, focusing on interest-crediting validation
/// and the average-balance interest computation. Repositories and unit-of-work are mocked.
/// </summary>
[TestFixture]
public class LedgerServiceTests
{
    private static (LedgerService svc,
                    Mock<ILedgerRepository> ledgerRepo,
                    Mock<IFundAccountRepository> accountRepo,
                    Mock<IUnitOfWork> uow)
        Build()
    {
        var http = new HttpClient(new StubHttpMessageHandler(_ => TestSupport.NotFound()))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var memberClient = new MemberServiceClient(http, Mock.Of<IHttpContextAccessor>());

        var ledgerRepo = new Mock<ILedgerRepository>();
        var accountRepo = new Mock<IFundAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        ledgerRepo.Setup(r => r.AddEntryAsync(It.IsAny<LedgerEntry>())).Returns(Task.CompletedTask);
        ledgerRepo.Setup(r => r.AddInterestRecordAsync(It.IsAny<InterestCreditRecord>())).Returns(Task.CompletedTask);

        var svc = new LedgerService(ledgerRepo.Object, accountRepo.Object, memberClient, uow.Object);
        return (svc, ledgerRepo, accountRepo, uow);
    }

    [Test]
    public void CreditInterest_EmptyAccountId_ThrowsArgument()
    {
        var (svc, _, _, _) = Build();
        Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreditInterestAsync(new CreditInterestRequest(Guid.Empty, "2025-26", 8m)));
    }

    [Test]
    public void CreditInterest_NonPositiveRate_ThrowsArgument()
    {
        var (svc, _, _, _) = Build();
        Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreditInterestAsync(new CreditInterestRequest(Guid.NewGuid(), "2025-26", 0m)));
    }

    [Test]
    public void CreditInterest_BadFormat_ThrowsArgument()
    {
        var (svc, _, _, _) = Build();
        Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreditInterestAsync(new CreditInterestRequest(Guid.NewGuid(), "2025", 8m)));
    }

    [Test]
    public void CreditInterest_BadSpan_ThrowsArgument()
    {
        var (svc, _, _, _) = Build();
        // 2025-27 spans two years -> invalid.
        Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreditInterestAsync(new CreditInterestRequest(Guid.NewGuid(), "2025-27", 8m)));
    }

    [Test]
    public void CreditInterest_AccountNotFound_ThrowsKeyNotFound()
    {
        var (svc, _, accountRepo, _) = Build();
        accountRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FundAccount?)null);
        Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.CreditInterestAsync(new CreditInterestRequest(Guid.NewGuid(), "2025-26", 8m)));
    }

    [Test]
    public void CreditInterest_AlreadyCredited_ThrowsInvalidOperation()
    {
        var (svc, ledgerRepo, accountRepo, _) = Build();
        var account = new FundAccount { TotalBalance = 100000m };
        accountRepo.Setup(r => r.FindByIdAsync(account.AccountId)).ReturnsAsync(account);
        ledgerRepo.Setup(r => r.InterestAlreadyCreditedAsync(account.AccountId, "2025-26")).ReturnsAsync(true);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreditInterestAsync(new CreditInterestRequest(account.AccountId, "2025-26", 8m)));
    }

    [Test]
    public async Task CreditInterest_Valid_ComputesAverageBalanceInterestAndUpdatesBalance()
    {
        var (svc, ledgerRepo, accountRepo, uow) = Build();
        var account = new FundAccount { TotalBalance = 110000m, InterestAccrued = 0m };
        accountRepo.Setup(r => r.FindByIdAsync(account.AccountId)).ReturnsAsync(account);
        ledgerRepo.Setup(r => r.InterestAlreadyCreditedAsync(account.AccountId, "2025-26")).ReturnsAsync(false);
        ledgerRepo.Setup(r => r.SumByTypeAsync(account.AccountId, EntryType.ContributionCredit)).ReturnsAsync(10000m);

        var result = await svc.CreditInterestAsync(new CreditInterestRequest(account.AccountId, "2025-26", 8m));

        // opening = 110000 - 10000 = 100000 ; interest = (100000 + 10000/2) * 8% = 8400
        Assert.That(result.OpeningBalance, Is.EqualTo(100000m));
        Assert.That(result.TotalContributions, Is.EqualTo(10000m));
        Assert.That(result.InterestAmount, Is.EqualTo(8400m));
        Assert.That(result.ClosingBalance, Is.EqualTo(118400m));
        Assert.That(account.TotalBalance, Is.EqualTo(118400m));
        Assert.That(account.InterestAccrued, Is.EqualTo(8400m));
        ledgerRepo.Verify(r => r.AddInterestRecordAsync(It.IsAny<InterestCreditRecord>()), Times.Once);
        ledgerRepo.Verify(r => r.AddEntryAsync(It.Is<LedgerEntry>(e => e.EntryType == EntryType.InterestCredit)), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
