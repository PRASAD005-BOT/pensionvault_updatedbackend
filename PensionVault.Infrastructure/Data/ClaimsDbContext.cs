using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;

namespace PensionVault.Infrastructure.Data;

public class ClaimsDbContext : DbContext
{
    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : base(options) { }

    public DbSet<BenefitClaim> BenefitClaims => Set<BenefitClaim>();
    public DbSet<ClaimDisbursement> ClaimDisbursements => Set<ClaimDisbursement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BenefitClaim
        modelBuilder.Entity<BenefitClaim>(e =>
        {
            e.HasKey(x => x.ClaimId);
            e.Property(x => x.ClaimType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EligibleAmount).HasPrecision(18, 2);
            e.Property(x => x.VestedAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxDeductible).HasPrecision(18, 2);
        });

        // ClaimDisbursement
        modelBuilder.Entity<ClaimDisbursement>(e =>
        {
            e.HasKey(x => x.DisbursementId);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.BankAccountRef).HasMaxLength(200);
            e.Property(x => x.DisbursedAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxDeducted).HasPrecision(18, 2);
            e.Property(x => x.NetAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Claim).WithMany(c => c.Disbursements)
                .HasForeignKey(x => x.ClaimId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
