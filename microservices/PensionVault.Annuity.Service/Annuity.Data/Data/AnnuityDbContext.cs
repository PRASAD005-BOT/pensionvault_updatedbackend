using Annuity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Annuity.Domain.Entities;

namespace Annuity.Data;

public class AnnuityDbContext : DbContext
{
    public AnnuityDbContext(DbContextOptions<AnnuityDbContext> options) : base(options) { }

    public DbSet<AnnuityPlan> AnnuityPlans => Set<AnnuityPlan>();
    public DbSet<AnnuityRequest> AnnuityRequests => Set<AnnuityRequest>();
    public DbSet<MonthlyPensionDisbursement> MonthlyPensionDisbursements => Set<MonthlyPensionDisbursement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AnnuityPlan
        modelBuilder.Entity<AnnuityPlan>(e =>
        {
            e.HasKey(x => x.AnnuityId);
            e.Property(x => x.PlanType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PurchaseValue).HasPrecision(18, 2);
            e.Property(x => x.MonthlyPension).HasPrecision(18, 2);
        });

        // MonthlyPensionDisbursement
        modelBuilder.Entity<MonthlyPensionDisbursement>(e =>
        {
            e.HasKey(x => x.DisbursementId);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.GrossAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxDeducted).HasPrecision(18, 2);
            e.Property(x => x.NetAmount).HasPrecision(18, 2);
            e.HasOne(x => x.AnnuityPlan).WithMany(a => a.PensionDisbursements)
                .HasForeignKey(x => x.AnnuityId).OnDelete(DeleteBehavior.Restrict);
        });

        // AnnuityRequest
        modelBuilder.Entity<AnnuityRequest>(e =>
        {
            e.HasKey(x => x.RequestId);
            e.Property(x => x.PlanType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PensionBalanceAtRequest).HasPrecision(18, 2);
            e.Property(x => x.EstimatedMonthly).HasPrecision(18, 2);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.ReviewNote).HasMaxLength(500);
        });
    }
}



