using Members.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Members.Domain.Entities;

namespace Members.Data.Seed;

public static class MembersDataSeeder
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
    public static readonly Guid FundAdminId    = Guid.Parse("f2a3b4c5-d6e7-8901-fa23-bc45de678901");
    public static readonly Guid ComplianceId   = Guid.Parse("a3b4c5d6-e7f8-9012-ab34-cd56ef789012");
    public static readonly Guid InvestOfficerId = Guid.Parse("b4c5d6e7-f8a9-0123-bc45-de67fa890123");

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
}



