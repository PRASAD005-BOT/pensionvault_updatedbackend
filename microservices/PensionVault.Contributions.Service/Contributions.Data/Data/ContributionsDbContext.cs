using Contributions.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Contributions.Domain.Entities;

namespace Contributions.Data;

public class ContributionsDbContext : DbContext
{
    public ContributionsDbContext(DbContextOptions<ContributionsDbContext> options) : base(options) { }

    public DbSet<FundAccount> FundAccounts => Set<FundAccount>();
    public DbSet<ContributionRemittance> ContributionRemittances => Set<ContributionRemittance>();
    public DbSet<MemberContribution> MemberContributions => Set<MemberContribution>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<InterestCreditRecord> InterestCreditRecords => Set<InterestCreditRecord>();
    public DbSet<InvestmentPortfolio> InvestmentPortfolios => Set<InvestmentPortfolio>();
    public DbSet<CorpusRecord> CorpusRecords => Set<CorpusRecord>();
    public DbSet<FundScheme> FundSchemes => Set<FundScheme>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // FundScheme (read-only reference in this service)
        modelBuilder.Entity<FundScheme>(e =>
        {
            e.HasKey(x => x.SchemeId);
            e.Property(x => x.SchemeName).HasMaxLength(150).IsRequired();
            e.Property(x => x.SchemeType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EmployeeContributionRate).HasPrecision(5, 2);
            e.Property(x => x.EmployerContributionRate).HasPrecision(5, 2);
            e.Property(x => x.InterestRatePA).HasPrecision(5, 2);
        });

        // FundAccount
        modelBuilder.Entity<FundAccount>(e =>
        {
            e.HasKey(x => x.AccountId);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EmployeeContributionBalance).HasPrecision(18, 2);
            e.Property(x => x.EmployerContributionBalance).HasPrecision(18, 2);
            e.Property(x => x.PensionBalance).HasPrecision(18, 2);
            e.Property(x => x.InterestAccrued).HasPrecision(18, 2);
            e.Property(x => x.TotalBalance).HasPrecision(18, 2);
            e.Property(x => x.VestingPercent).HasPrecision(5, 2);
            e.HasOne(x => x.Scheme).WithMany()
                .HasForeignKey(x => x.SchemeId).OnDelete(DeleteBehavior.Restrict);
        });

        // ContributionRemittance
        modelBuilder.Entity<ContributionRemittance>(e =>
        {
            e.HasKey(x => x.RemittanceId);
            e.Property(x => x.RemittancePeriod).HasMaxLength(10).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.TotalEmployeeShare).HasPrecision(18, 2);
            e.Property(x => x.TotalEmployerShare).HasPrecision(18, 2);
            e.Property(x => x.TotalPensionAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
        });

        // MemberContribution
        modelBuilder.Entity<MemberContribution>(e =>
        {
            e.HasKey(x => x.ContributionId);
            e.Property(x => x.Period).HasMaxLength(10).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EmployeeAmount).HasPrecision(18, 2);
            e.Property(x => x.EmployerAmount).HasPrecision(18, 2);
            e.Property(x => x.PensionAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Remittance).WithMany(r => r.MemberContributions)
                .HasForeignKey(x => x.RemittanceId).OnDelete(DeleteBehavior.Restrict);
        });

        // LedgerEntry
        modelBuilder.Entity<LedgerEntry>(e =>
        {
            e.HasKey(x => x.EntryId);
            e.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ReferenceId).HasMaxLength(100);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            e.HasOne(x => x.Account).WithMany(a => a.LedgerEntries)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        // InterestCreditRecord
        modelBuilder.Entity<InterestCreditRecord>(e =>
        {
            e.HasKey(x => x.InterestId);
            e.Property(x => x.FinancialYear).HasMaxLength(10).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            e.Property(x => x.TotalContributions).HasPrecision(18, 2);
            e.Property(x => x.InterestRateApplied).HasPrecision(5, 2);
            e.Property(x => x.InterestAmount).HasPrecision(18, 2);
            e.Property(x => x.ClosingBalance).HasPrecision(18, 2);
            e.HasOne(x => x.Account).WithMany(a => a.InterestRecords)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        // InvestmentPortfolio
        modelBuilder.Entity<InvestmentPortfolio>(e =>
        {
            e.HasKey(x => x.PortfolioId);
            e.Property(x => x.AssetClass).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.AllocationPercent).HasPrecision(5, 2);
            e.Property(x => x.InvestedValue).HasPrecision(18, 2);
            e.Property(x => x.CurrentValue).HasPrecision(18, 2);
            e.Property(x => x.YieldEarned).HasPrecision(18, 2);
            e.HasOne(x => x.Scheme).WithMany()
                .HasForeignKey(x => x.SchemeId).OnDelete(DeleteBehavior.Restrict);
        });

        // CorpusRecord
        modelBuilder.Entity<CorpusRecord>(e =>
        {
            e.HasKey(x => x.CorpusId);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.TotalContributions).HasPrecision(18, 2);
            e.Property(x => x.TotalWithdrawals).HasPrecision(18, 2);
            e.Property(x => x.InvestmentIncome).HasPrecision(18, 2);
            e.Property(x => x.ManagementExpenses).HasPrecision(18, 2);
            e.Property(x => x.ClosingCorpus).HasPrecision(18, 2);
            e.HasOne(x => x.Scheme).WithMany()
                .HasForeignKey(x => x.SchemeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}



