using Contributions.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Contributions.Data;
using Contributions.Domain.Entities;

namespace Contributions.Data.Seed;

public static class ContributionsDataSeeder
{
    public static readonly Guid EpfSchemeId    = Guid.Parse("b2c3d4e5-f6a7-8901-bc23-de45fa678901");
    public static readonly Guid GratuitySchemeId = Guid.Parse("c3d4e5f6-a7b8-9012-cd34-ef56ab789012");
    public static readonly Guid Member1Id      = Guid.Parse("a7b8c9d0-e1f2-3456-ab78-cd90ef123456");
    public static readonly Guid Member2Id      = Guid.Parse("c9d0e1f2-a3b4-5678-cd90-ef12ab345678");
    public static readonly Guid Account1Id     = Guid.Parse("d0e1f2a3-b4c5-6789-de01-fa23bc456789");
    public static readonly Guid Account2Id     = Guid.Parse("e1f2a3b4-c5d6-7890-ef12-ab34cd567890");

    public static async Task SeedAsync(ContributionsDbContext context)
    {
        if (await context.FundSchemes.AnyAsync()) return;

        var epfScheme = new FundScheme
        {
            SchemeId = EpfSchemeId,
            SchemeName = "Employee Provident Fund",
            SchemeType = SchemeType.EPF,
            EmployeeContributionRate = 12.00m,
            EmployerContributionRate = 12.00m,
            InterestRatePA = 8.15m,
            VestingSchedule = "{\"years\": 5, \"percent\": 100}",
            Status = SchemeStatus.Active
        };
        var gratuityScheme = new FundScheme
        {
            SchemeId = GratuitySchemeId,
            SchemeName = "Gratuity Trust Fund",
            SchemeType = SchemeType.Gratuity,
            EmployeeContributionRate = 0.00m,
            EmployerContributionRate = 4.81m,
            InterestRatePA = 7.50m,
            VestingSchedule = "{\"years\": 5, \"percent\": 100}",
            Status = SchemeStatus.Active
        };
        context.FundSchemes.AddRange(epfScheme, gratuityScheme);

        var account1 = new FundAccount
        {
            AccountId = Account1Id,
            MemberId = Member1Id,
            SchemeId = epfScheme.SchemeId,
            AccountOpenDate = new DateTime(2020, 1, 1),
            EmployeeContributionBalance = 120000.00m,
            EmployerContributionBalance = 20000.00m,
            PensionBalance = 100000.00m,
            InterestAccrued = 32800.00m,
            TotalBalance = 172800.00m,
            VestingPercent = 100,
            Status = FundAccountStatus.Active
        };
        var account2 = new FundAccount
        {
            AccountId = Account2Id,
            MemberId = Member2Id,
            SchemeId = epfScheme.SchemeId,
            AccountOpenDate = new DateTime(2021, 4, 1),
            EmployeeContributionBalance = 72000.00m,
            EmployerContributionBalance = 72000.00m,
            InterestAccrued = 15600.00m,
            TotalBalance = 159600.00m,
            VestingPercent = 100,
            Status = FundAccountStatus.Active
        };
        context.FundAccounts.AddRange(account1, account2);

        context.InvestmentPortfolios.AddRange(
            new InvestmentPortfolio
            {
                PortfolioId = Guid.NewGuid(),
                SchemeId = epfScheme.SchemeId,
                AssetClass = AssetClass.GovernmentSecurities,
                AllocationPercent = 45.00m,
                InvestedValue = 5000000m,
                CurrentValue = 5250000m,
                YieldEarned = 250000m,
                LastUpdated = DateTime.UtcNow
            },
            new InvestmentPortfolio
            {
                PortfolioId = Guid.NewGuid(),
                SchemeId = epfScheme.SchemeId,
                AssetClass = AssetClass.CorporateBonds,
                AllocationPercent = 30.00m,
                InvestedValue = 3000000m,
                CurrentValue = 3150000m,
                YieldEarned = 150000m,
                LastUpdated = DateTime.UtcNow
            },
            new InvestmentPortfolio
            {
                PortfolioId = Guid.NewGuid(),
                SchemeId = epfScheme.SchemeId,
                AssetClass = AssetClass.Equity,
                AllocationPercent = 15.00m,
                InvestedValue = 1500000m,
                CurrentValue = 1650000m,
                YieldEarned = 150000m,
                LastUpdated = DateTime.UtcNow
            },
            new InvestmentPortfolio
            {
                PortfolioId = Guid.NewGuid(),
                SchemeId = epfScheme.SchemeId,
                AssetClass = AssetClass.FixedDeposit,
                AllocationPercent = 10.00m,
                InvestedValue = 1000000m,
                CurrentValue = 1080000m,
                YieldEarned = 80000m,
                LastUpdated = DateTime.UtcNow
            }
        );

        context.CorpusRecords.Add(new CorpusRecord
        {
            CorpusId = Guid.NewGuid(),
            SchemeId = epfScheme.SchemeId,
            RecordDate = new DateTime(2024, 3, 31),
            TotalContributions = 10500000m,
            TotalWithdrawals = 500000m,
            InvestmentIncome = 630000m,
            ManagementExpenses = 50000m,
            ClosingCorpus = 10580000m,
            Status = CorpusStatus.Finalised
        });

        await context.SaveChangesAsync();
    }
}



