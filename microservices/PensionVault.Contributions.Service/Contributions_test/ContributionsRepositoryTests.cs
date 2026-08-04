using Contributions.Data;
using Contributions.Data.Repositories;
using Contributions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Contributions_test;

/// <summary>
/// Repository tests for <see cref="LedgerRepository"/>, <see cref="InvestmentRepository"/>
/// and <see cref="FundAccountRepository"/> using the EF Core in-memory provider.
/// </summary>
[TestFixture]
public class ContributionsRepositoryTests
{
    private static ContributionsDbContext NewContext()
        => new(new DbContextOptionsBuilder<ContributionsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // ---------------- LedgerRepository ----------------

    [Test]
    public async Task Ledger_AddEntry_GetByAccount_ReturnsAccountEntries()
    {
        using var ctx = NewContext();
        var repo = new LedgerRepository(ctx);
        var accountId = Guid.NewGuid();
        await repo.AddEntryAsync(new LedgerEntry { AccountId = accountId, EntryType = EntryType.ContributionCredit, Amount = 1000m, BalanceAfter = 1000m });
        await repo.AddEntryAsync(new LedgerEntry { AccountId = Guid.NewGuid(), EntryType = EntryType.ContributionCredit, Amount = 5000m, BalanceAfter = 5000m });
        await ctx.SaveChangesAsync();

        var entries = await repo.GetByAccountAsync(accountId);
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Amount, Is.EqualTo(1000m));
    }

    [Test]
    public async Task Ledger_SumByType_SumsOnlyPostedOfThatType()
    {
        using var ctx = NewContext();
        var repo = new LedgerRepository(ctx);
        var accountId = Guid.NewGuid();
        await repo.AddEntryAsync(new LedgerEntry { AccountId = accountId, EntryType = EntryType.ContributionCredit, Amount = 1000m, Status = LedgerEntryStatus.Posted });
        await repo.AddEntryAsync(new LedgerEntry { AccountId = accountId, EntryType = EntryType.ContributionCredit, Amount = 2000m, Status = LedgerEntryStatus.Posted });
        await repo.AddEntryAsync(new LedgerEntry { AccountId = accountId, EntryType = EntryType.ContributionCredit, Amount = 9000m, Status = LedgerEntryStatus.Reversed });
        await repo.AddEntryAsync(new LedgerEntry { AccountId = accountId, EntryType = EntryType.ClaimDebit, Amount = 500m, Status = LedgerEntryStatus.Posted });
        await ctx.SaveChangesAsync();

        var sum = await repo.SumByTypeAsync(accountId, EntryType.ContributionCredit);
        Assert.That(sum, Is.EqualTo(3000m));   // only the two posted contribution credits
    }

    [Test]
    public async Task Ledger_InterestAlreadyCredited_TrueOnlyForCreditedYear()
    {
        using var ctx = NewContext();
        var repo = new LedgerRepository(ctx);
        var accountId = Guid.NewGuid();
        await repo.AddInterestRecordAsync(new InterestCreditRecord
        { AccountId = accountId, FinancialYear = "2025-26", Status = InterestCreditStatus.Credited });
        await ctx.SaveChangesAsync();

        Assert.That(await repo.InterestAlreadyCreditedAsync(accountId, "2025-26"), Is.True);
        Assert.That(await repo.InterestAlreadyCreditedAsync(accountId, "2024-25"), Is.False);
    }

    // ---------------- InvestmentRepository ----------------

    [Test]
    public async Task Investment_AddPortfolio_GetPortfolios_FiltersByScheme()
    {
        using var ctx = NewContext();
        var repo = new InvestmentRepository(ctx);
        var schemeId = Guid.NewGuid();
        await repo.AddSchemeAsync(new FundScheme { SchemeId = schemeId, SchemeName = "EPF" });
        await repo.AddPortfolioAsync(new InvestmentPortfolio { SchemeId = schemeId, AssetClass = AssetClass.Equity, AllocationPercent = 40m });
        await repo.AddPortfolioAsync(new InvestmentPortfolio { SchemeId = Guid.NewGuid(), AssetClass = AssetClass.Equity, AllocationPercent = 60m });
        await ctx.SaveChangesAsync();

        var list = await repo.GetPortfoliosAsync(schemeId);
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0].AllocationPercent, Is.EqualTo(40m));
    }

    [Test]
    public async Task Investment_SchemeExists_ReflectsData()
    {
        using var ctx = NewContext();
        var repo = new InvestmentRepository(ctx);
        var schemeId = Guid.NewGuid();
        await repo.AddSchemeAsync(new FundScheme { SchemeId = schemeId, SchemeName = "EPF" });
        await ctx.SaveChangesAsync();

        Assert.That(await repo.SchemeExistsAsync(schemeId), Is.True);
        Assert.That(await repo.SchemeExistsAsync(Guid.NewGuid()), Is.False);
    }

    [Test]
    public async Task Investment_GetLastFinalisedCorpus_ReturnsLatestFinalisedOnly()
    {
        using var ctx = NewContext();
        var repo = new InvestmentRepository(ctx);
        var schemeId = Guid.NewGuid();
        await repo.AddSchemeAsync(new FundScheme { SchemeId = schemeId, SchemeName = "EPF" });
        await repo.AddCorpusAsync(new CorpusRecord { SchemeId = schemeId, RecordDate = new DateTime(2024, 1, 1), ClosingCorpus = 1000m, Status = CorpusStatus.Finalised });
        await repo.AddCorpusAsync(new CorpusRecord { SchemeId = schemeId, RecordDate = new DateTime(2025, 1, 1), ClosingCorpus = 2000m, Status = CorpusStatus.Finalised });
        await repo.AddCorpusAsync(new CorpusRecord { SchemeId = schemeId, RecordDate = new DateTime(2026, 1, 1), ClosingCorpus = 9999m, Status = CorpusStatus.Draft });
        await ctx.SaveChangesAsync();

        var last = await repo.GetLastFinalisedCorpusAsync(schemeId);
        Assert.That(last, Is.Not.Null);
        Assert.That(last!.ClosingCorpus, Is.EqualTo(2000m));   // latest finalised, draft ignored
    }

    // ---------------- FundAccountRepository ----------------

    [Test]
    public async Task FundAccount_AddAsync_FindById_ReturnsAccount()
    {
        using var ctx = NewContext();
        var repo = new FundAccountRepository(ctx);
        var schemeId = Guid.NewGuid();
        await ctx.FundSchemes.AddAsync(new FundScheme { SchemeId = schemeId, SchemeName = "EPF" });
        var account = new FundAccount { MemberId = Guid.NewGuid(), SchemeId = schemeId, TotalBalance = 5000m };
        await repo.AddAsync(account);
        await ctx.SaveChangesAsync();

        var found = await repo.FindByIdAsync(account.AccountId);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.TotalBalance, Is.EqualTo(5000m));
    }

    [Test]
    public async Task FundAccount_FindActiveByMember_ReturnsActiveOnly()
    {
        using var ctx = NewContext();
        var repo = new FundAccountRepository(ctx);
        var schemeId1 = Guid.NewGuid();
        var schemeId2 = Guid.NewGuid();
        await ctx.FundSchemes.AddRangeAsync(
            new FundScheme { SchemeId = schemeId1, SchemeName = "EPF" },
            new FundScheme { SchemeId = schemeId2, SchemeName = "Gratuity" });
        var memberId = Guid.NewGuid();
        await repo.AddAsync(new FundAccount { MemberId = memberId, SchemeId = schemeId1, Status = FundAccountStatus.Settled });
        var active = new FundAccount { MemberId = memberId, SchemeId = schemeId2, Status = FundAccountStatus.Active };
        await repo.AddAsync(active);
        await ctx.SaveChangesAsync();

        var found = await repo.FindActiveByMemberAsync(memberId);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.AccountId, Is.EqualTo(active.AccountId));
    }

    [Test]
    public async Task FundAccount_ExistsByMember_And_GetActiveScheme()
    {
        using var ctx = NewContext();
        var repo = new FundAccountRepository(ctx);
        var memberId = Guid.NewGuid();
        await repo.AddAsync(new FundAccount { MemberId = memberId, SchemeId = Guid.NewGuid() });
        await ctx.FundSchemes.AddAsync(new FundScheme { SchemeName = "EPF", Status = SchemeStatus.Active });
        await ctx.SaveChangesAsync();

        Assert.That(await repo.ExistsByMemberAsync(memberId), Is.True);
        Assert.That(await repo.ExistsByMemberAsync(Guid.NewGuid()), Is.False);
        Assert.That(await repo.GetActiveSchemeAsync(), Is.Not.Null);
    }
}
