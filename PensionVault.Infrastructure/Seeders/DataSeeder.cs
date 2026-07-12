using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Seeders;

public static class DataSeeder
{
    public static readonly Guid AdminId        = Guid.Parse("a1b2c3d4-e5f6-7890-ab12-cd34ef567890");
    public static readonly Guid EpfSchemeId    = Guid.Parse("b2c3d4e5-f6a7-8901-bc23-de45fa678901");
    public static readonly Guid GratuitySchemeId = Guid.Parse("c3d4e5f6-a7b8-9012-cd34-ef56ab789012");
    public static readonly Guid AcmeEmployerId = Guid.Parse("d4e5f6a7-b8c9-0123-de45-fa67bc890123");
    public static readonly Guid EmployerUserId = Guid.Parse("e5f6a7b8-c9d0-1234-ef56-ab78cd901234");
    public static readonly Guid MemberUser1Id  = Guid.Parse("f6a7b8c9-d0e1-2345-fa67-bc89de012345");
    public static readonly Guid Member1Id      = Guid.Parse("a7b8c9d0-e1f2-3456-ab78-cd90ef123456");
    public static readonly Guid MemberUser2Id  = Guid.Parse("b8c9d0e1-f2a3-4567-bc89-de01fa234567");
    public static readonly Guid Member2Id      = Guid.Parse("c9d0e1f2-a3b4-5678-cd90-ef12ab345678");
    public static readonly Guid Account1Id     = Guid.Parse("d0e1f2a3-b4c5-6789-de01-fa23bc456789");
    public static readonly Guid Account2Id     = Guid.Parse("e1f2a3b4-c5d6-7890-ef12-ab34cd567890");
    public static readonly Guid FundAdminId    = Guid.Parse("f2a3b4c5-d6e7-8901-fa23-bc45de678901");
    public static readonly Guid ComplianceId   = Guid.Parse("a3b4c5d6-e7f8-9012-ab34-cd56ef789012");
    public static readonly Guid InvestOfficerId = Guid.Parse("b4c5d6e7-f8a9-0123-bc45-de67fa890123");

    // ── Original Monolith Seeder ──────────────────────────────────────────
    public static async Task SeedAsync(AppDbContext context, IConfiguration configuration)
    {
        if (await context.Users.AnyAsync()) return;

        var admin = new User
        {
            UserId = AdminId,
            Name = "System Administrator",
            Email = "admin@pensionvault.com",
            Role = UserRole.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:AdminPassword"] ?? "Admin@123"),
            Status = UserStatus.Active
        };
        context.Users.Add(admin);

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

        var employer = new Employer
        {
            EmployerId = AcmeEmployerId,
            CompanyName = "Acme Technologies Pvt Ltd",
            RegistrationNumber = "CIN-L99999MH2010PLC123456",
            Industry = "Information Technology",
            EnrolledMemberCount = 3,
            RemittanceFrequency = RemittanceFrequency.Monthly,
            ContactDetails = "{\"email\":\"hr@acmetech.com\",\"phone\":\"+91-22-12345678\"}",
            Status = EmployerStatus.Active
        };
        var employerUser = new User
        {
            UserId = EmployerUserId,
            Name = "Acme HR Manager",
            Email = "hr@acmetech.com",
            Role = UserRole.Employer,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:EmployerPassword"] ?? "Employer@123"),
            OrganisationId = employer.EmployerId,
            Status = UserStatus.Active
        };
        context.Employers.Add(employer);
        context.Users.Add(employerUser);

        var memberUser1 = new User
        {
            UserId = MemberUser1Id,
            Name = "Rajesh Kumar",
            Email = "rajesh.kumar@acmetech.com",
            Role = UserRole.Member,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:MemberPassword"] ?? "Member@123"),
            OrganisationId = employer.EmployerId,
            EmployeeId = "EMP-1001",
            Status = UserStatus.Active
        };
        var member1 = new Member
        {
            MemberId = Member1Id,
            UserId = memberUser1.UserId,
            MembershipNumber = "PV-2024-00001",
            Name = "Rajesh Kumar",
            DateOfBirth = new DateTime(1985, 6, 15),
            Gender = "Male",
            NationalIdRef = "AAAPK1234C",
            EmployerId = employer.EmployerId,
            JoiningDate = new DateTime(2020, 1, 1),
            DateOfRetirement = new DateTime(2045, 6, 15),
            NomineeDetails = "{\"name\":\"Priya Kumar\",\"relation\":\"Spouse\",\"percent\":100}",
            Status = MemberStatus.Active
        };
        var memberUser2 = new User
        {
            UserId = MemberUser2Id,
            Name = "Priya Sharma",
            Email = "priya.sharma@acmetech.com",
            Role = UserRole.Member,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:MemberPassword"] ?? "Member@123"),
            OrganisationId = employer.EmployerId,
            EmployeeId = "EMP-1002",
            Status = UserStatus.Active
        };
        var member2 = new Member
        {
            MemberId = Member2Id,
            UserId = memberUser2.UserId,
            MembershipNumber = "PV-2024-00002",
            Name = "Priya Sharma",
            DateOfBirth = new DateTime(1990, 3, 22),
            Gender = "Female",
            NationalIdRef = "BBBPK5678D",
            EmployerId = employer.EmployerId,
            JoiningDate = new DateTime(2021, 4, 1),
            DateOfRetirement = new DateTime(2050, 3, 22),
            NomineeDetails = "{\"name\":\"Amit Sharma\",\"relation\":\"Spouse\",\"percent\":100}",
            Status = MemberStatus.Active
        };
        context.Users.AddRange(memberUser1, memberUser2);
        context.Members.AddRange(member1, member2);

        var account1 = new FundAccount
        {
            AccountId = Account1Id,
            MemberId = member1.MemberId,
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
            MemberId = member2.MemberId,
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

        var fundAdmin = new User
        {
            UserId = FundAdminId,
            Name = "Fund Administrator",
            Email = "fundadmin@pensionvault.com",
            Role = UserRole.FundAdmin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:FundAdminPassword"] ?? "FundAdmin@123"),
            Status = UserStatus.Active
        };
        var compliance = new User
        {
            UserId = ComplianceId,
            Name = "Compliance Officer",
            Email = "compliance@pensionvault.com",
            Role = UserRole.Compliance,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:CompliancePassword"] ?? "Compliance@123"),
            Status = UserStatus.Active
        };
        var investOfficer = new User
        {
            UserId = InvestOfficerId,
            Name = "Investment Officer",
            Email = "investment@pensionvault.com",
            Role = UserRole.InvestmentOfficer,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:InvestmentOfficerPassword"] ?? "Invest@123"),
            Status = UserStatus.Active
        };
        context.Users.AddRange(fundAdmin, compliance, investOfficer);

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

        context.Notifications.AddRange(
            new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = memberUser1.UserId,
                Message = "Welcome to PensionVault! Your EPF account has been created with balance ₹2,72,800.",
                Category = NotificationCategory.Contribution,
                Status = NotificationStatus.Unread,
                CreatedDate = DateTime.UtcNow
            },
            new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = memberUser2.UserId,
                Message = "Welcome to PensionVault! Your EPF account has been created with balance ₹1,59,600.",
                Category = NotificationCategory.Contribution,
                Status = NotificationStatus.Unread,
                CreatedDate = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();
    }

    // ── Members DB Seeder ───────────────────────────────────────────────
    public static async Task SeedAsync(MembersDbContext context, IConfiguration configuration)
    {
        if (await context.Users.AnyAsync()) return;

        var admin = new User
        {
            UserId = AdminId,
            Name = "System Administrator",
            Email = "admin@pensionvault.com",
            Role = UserRole.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:AdminPassword"] ?? "Admin@123"),
            Status = UserStatus.Active
        };
        context.Users.Add(admin);

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

        var employer = new Employer
        {
            EmployerId = AcmeEmployerId,
            CompanyName = "Acme Technologies Pvt Ltd",
            RegistrationNumber = "CIN-L99999MH2010PLC123456",
            Industry = "Information Technology",
            EnrolledMemberCount = 3,
            RemittanceFrequency = RemittanceFrequency.Monthly,
            ContactDetails = "{\"email\":\"hr@acmetech.com\",\"phone\":\"+91-22-12345678\"}",
            Status = EmployerStatus.Active
        };
        var employerUser = new User
        {
            UserId = EmployerUserId,
            Name = "Acme HR Manager",
            Email = "hr@acmetech.com",
            Role = UserRole.Employer,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:EmployerPassword"] ?? "Employer@123"),
            OrganisationId = employer.EmployerId,
            Status = UserStatus.Active
        };
        context.Employers.Add(employer);
        context.Users.Add(employerUser);

        var memberUser1 = new User
        {
            UserId = MemberUser1Id,
            Name = "Rajesh Kumar",
            Email = "rajesh.kumar@acmetech.com",
            Role = UserRole.Member,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:MemberPassword"] ?? "Member@123"),
            OrganisationId = employer.EmployerId,
            EmployeeId = "EMP-1001",
            Status = UserStatus.Active
        };
        var member1 = new Member
        {
            MemberId = Member1Id,
            UserId = memberUser1.UserId,
            MembershipNumber = "PV-2024-00001",
            Name = "Rajesh Kumar",
            DateOfBirth = new DateTime(1985, 6, 15),
            Gender = "Male",
            NationalIdRef = "AAAPK1234C",
            EmployerId = employer.EmployerId,
            JoiningDate = new DateTime(2020, 1, 1),
            DateOfRetirement = new DateTime(2045, 6, 15),
            NomineeDetails = "{\"name\":\"Priya Kumar\",\"relation\":\"Spouse\",\"percent\":100}",
            Status = MemberStatus.Active
        };
        var memberUser2 = new User
        {
            UserId = MemberUser2Id,
            Name = "Priya Sharma",
            Email = "priya.sharma@acmetech.com",
            Role = UserRole.Member,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:MemberPassword"] ?? "Member@123"),
            OrganisationId = employer.EmployerId,
            EmployeeId = "EMP-1002",
            Status = UserStatus.Active
        };
        var member2 = new Member
        {
            MemberId = Member2Id,
            UserId = memberUser2.UserId,
            MembershipNumber = "PV-2024-00002",
            Name = "Priya Sharma",
            DateOfBirth = new DateTime(1990, 3, 22),
            Gender = "Female",
            NationalIdRef = "BBBPK5678D",
            EmployerId = employer.EmployerId,
            JoiningDate = new DateTime(2021, 4, 1),
            DateOfRetirement = new DateTime(2050, 3, 22),
            NomineeDetails = "{\"name\":\"Amit Sharma\",\"relation\":\"Spouse\",\"percent\":100}",
            Status = MemberStatus.Active
        };
        context.Users.AddRange(memberUser1, memberUser2);
        context.Members.AddRange(member1, member2);

        var fundAdmin = new User
        {
            UserId = FundAdminId,
            Name = "Fund Administrator",
            Email = "fundadmin@pensionvault.com",
            Role = UserRole.FundAdmin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:FundAdminPassword"] ?? "FundAdmin@123"),
            Status = UserStatus.Active
        };
        var compliance = new User
        {
            UserId = ComplianceId,
            Name = "Compliance Officer",
            Email = "compliance@pensionvault.com",
            Role = UserRole.Compliance,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:CompliancePassword"] ?? "Compliance@123"),
            Status = UserStatus.Active
        };
        var investOfficer = new User
        {
            UserId = InvestOfficerId,
            Name = "Investment Officer",
            Email = "investment@pensionvault.com",
            Role = UserRole.InvestmentOfficer,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["SeederCredentials:InvestmentOfficerPassword"] ?? "Invest@123"),
            Status = UserStatus.Active
        };
        context.Users.AddRange(fundAdmin, compliance, investOfficer);

        context.Notifications.AddRange(
            new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = memberUser1.UserId,
                Message = "Welcome to PensionVault! Your EPF account has been created with balance ₹2,72,800.",
                Category = NotificationCategory.Contribution,
                Status = NotificationStatus.Unread,
                CreatedDate = DateTime.UtcNow
            },
            new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = memberUser2.UserId,
                Message = "Welcome to PensionVault! Your EPF account has been created with balance ₹1,59,600.",
                Category = NotificationCategory.Contribution,
                Status = NotificationStatus.Unread,
                CreatedDate = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();
    }

    // ── Contributions DB Seeder ──────────────────────────────────────────
    public static async Task SeedAsync(ContributionsDbContext context, IConfiguration configuration)
    {
        if (await context.FundAccounts.AnyAsync()) return;

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

    // ── Annuity DB Seeder ────────────────────────────────────────────────
    public static async Task SeedAsync(AnnuityDbContext context, IConfiguration configuration)
    {
        if (await context.AnnuityPlans.AnyAsync()) return;
        await context.SaveChangesAsync();
    }

    // ── Claims DB Seeder ─────────────────────────────────────────────────
    public static async Task SeedAsync(ClaimsDbContext context, IConfiguration configuration)
    {
        if (await context.BenefitClaims.AnyAsync()) return;
        await context.SaveChangesAsync();
    }
}
