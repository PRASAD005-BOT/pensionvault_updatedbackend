using Members.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Members.Domain.Entities;

namespace Members.Data;

public class MembersDbContext : DbContext
{
    public MembersDbContext(DbContextOptions<MembersDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Employer> Employers => Set<Employer>();
    public DbSet<FundScheme> FundSchemes => Set<FundScheme>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.UserId);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.AuditId);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.RecordId).HasMaxLength(100);
            e.HasOne(x => x.User).WithMany(u => u.AuditLogs)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // FundScheme
        modelBuilder.Entity<FundScheme>(e =>
        {
            e.HasKey(x => x.SchemeId);
            e.Property(x => x.SchemeName).HasMaxLength(150).IsRequired();
            e.Property(x => x.SchemeType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EmployeeContributionRate).HasPrecision(5, 2);
            e.Property(x => x.EmployerContributionRate).HasPrecision(5, 2);
            e.Property(x => x.InterestRatePA).HasPrecision(5, 2);
            e.Property(x => x.VestingYears);
            e.Property(x => x.VestingPercent).HasPrecision(5, 2);
        });

        // Employer
        modelBuilder.Entity<Employer>(e =>
        {
            e.HasKey(x => x.EmployerId);
            e.HasIndex(x => x.RegistrationNumber).IsUnique();
            e.HasIndex(x => x.EmployerCode).IsUnique();
            e.Property(x => x.EmployerCode).HasMaxLength(20);
            e.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
            e.Property(x => x.RegistrationNumber).HasMaxLength(100);
            e.Property(x => x.Industry).HasMaxLength(100);
            e.Property(x => x.RemittanceFrequency).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ContactEmail).HasMaxLength(200);
            e.Property(x => x.ContactPhone).HasMaxLength(20);
            e.Property(x => x.PortalJoinCode).HasMaxLength(50);
        });

        // Member
        modelBuilder.Entity<Member>(e =>
        {
            e.HasKey(x => x.MemberId);
            e.HasIndex(x => x.MembershipNumber).IsUnique();
            e.Property(x => x.MembershipNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Gender).HasMaxLength(10);
            e.Property(x => x.NationalIdRef).HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.NomineeName).HasMaxLength(150);
            e.Property(x => x.NomineeRelation).HasMaxLength(50);
            e.Property(x => x.NomineeBankAccount).HasMaxLength(100);
            e.Property(x => x.NomineePercent);
            e.HasOne(x => x.User).WithOne(u => u.Member)
                .HasForeignKey<Member>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Employer).WithMany(emp => emp.Members)
                .HasForeignKey(x => x.EmployerId).OnDelete(DeleteBehavior.Restrict);
        });

        // Notification
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.NotificationId);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.User).WithMany(u => u.Notifications)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}



