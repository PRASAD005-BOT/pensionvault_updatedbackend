using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;

namespace PensionVault.Infrastructure.Data;

/// <summary>
/// Lightweight DbContext that only exposes AuditLogs, used by non-Members microservices
/// to write audit trail entries to the central Members database.
/// Does NOT reference User or any other navigation to avoid pulling unrelated entities
/// into this context's model.
/// </summary>
public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.AuditId);
            e.ToTable("AuditLogs");
            e.Property(x => x.UserId);          // plain FK column, no navigation
            e.Property(x => x.Action).HasMaxLength(200);
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.RecordId).HasMaxLength(100);
            e.Property(x => x.Timestamp);
            // No HasOne<User>() — avoids pulling in the entire User graph
            // (Members, AnnuityPlans, etc.) which causes EF model errors
            // in databases that don't have those tables configured.
        });
    }
}
