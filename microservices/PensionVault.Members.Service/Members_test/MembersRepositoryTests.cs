using Members.Data;
using Members.Data.Repositories;
using Members.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Members_test;

/// <summary>
/// Repository tests for <see cref="MemberRepository"/>, <see cref="EmployerRepository"/>,
/// and <see cref="FundSchemeRepository"/> using the EF Core in-memory provider.
/// </summary>
[TestFixture]
public class MembersRepositoryTests
{
    private static MembersDbContext NewContext()
        => new(new DbContextOptionsBuilder<MembersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // ---------- MemberRepository ----------

    [Test]
    public async Task Member_AddAsync_FindById_ReturnsMember()
    {
        using var ctx = NewContext();
        var repo = new MemberRepository(ctx);
        var empId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await ctx.Employers.AddAsync(new Employer { EmployerId = empId, EmployerCode = "TEST", CompanyName = "Test Co" });
        await ctx.Users.AddAsync(new User { UserId = userId, Name = "John", Email = "john@test.com", Role = UserRole.Member, PasswordHash = "hash" });
        var member = new Member { UserId = userId, MembershipNumber = "MEM001", Name = "John", EmployerId = empId, DateOfBirth = DateTime.Today.AddYears(-30), JoiningDate = DateTime.Today };
        await repo.AddAsync(member);
        await ctx.SaveChangesAsync();

        var found = await repo.FindByIdAsync(member.MemberId);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("John"));
    }

    [Test]
    public async Task Member_ExistsByMembershipNumber_ReflectsData()
    {
        using var ctx = NewContext();
        var repo = new MemberRepository(ctx);
        var empId = Guid.NewGuid();
        await ctx.Employers.AddAsync(new Employer { EmployerId = empId, EmployerCode = "TEST", CompanyName = "Test" });
        var member = new Member { UserId = Guid.NewGuid(), MembershipNumber = "MEM001", Name = "John", EmployerId = empId };
        await repo.AddAsync(member);
        await ctx.SaveChangesAsync();

        Assert.That(await repo.ExistsByMembershipNumberAsync("MEM001"), Is.True);
        Assert.That(await repo.ExistsByMembershipNumberAsync("MEM999"), Is.False);
    }

    [Test]
    public async Task Member_ExistsByMembershipNumber_ExcludesId()
    {
        using var ctx = NewContext();
        var repo = new MemberRepository(ctx);
        var empId = Guid.NewGuid();
        await ctx.Employers.AddAsync(new Employer { EmployerId = empId, EmployerCode = "TEST", CompanyName = "Test" });
        var member = new Member { UserId = Guid.NewGuid(), MembershipNumber = "MEM001", Name = "John", EmployerId = empId };
        await repo.AddAsync(member);
        await ctx.SaveChangesAsync();

        // Exclude the member itself -> should be false (no duplicate)
        Assert.That(await repo.ExistsByMembershipNumberAsync("MEM001", member.MemberId), Is.False);
    }

    [Test]
    public async Task Member_GetAllByEmployer_FiltersCorrectly()
    {
        using var ctx = NewContext();
        var repo = new MemberRepository(ctx);
        var emp1 = Guid.NewGuid();
        var emp2 = Guid.NewGuid();
        await ctx.Employers.AddRangeAsync(
            new Employer { EmployerId = emp1, EmployerCode = "EMP1", CompanyName = "Co1" },
            new Employer { EmployerId = emp2, EmployerCode = "EMP2", CompanyName = "Co2" });
        await repo.AddAsync(new Member { UserId = Guid.NewGuid(), MembershipNumber = "M001", Name = "John", EmployerId = emp1 });
        await repo.AddAsync(new Member { UserId = Guid.NewGuid(), MembershipNumber = "M002", Name = "Jane", EmployerId = emp2 });
        await ctx.SaveChangesAsync();

        var emp1Members = await repo.GetAllAsync(emp1);
        Assert.That(emp1Members, Has.Count.EqualTo(1));
        Assert.That(emp1Members[0].Name, Is.EqualTo("John"));
    }

    // ---------- EmployerRepository ----------

    [Test]
    public async Task Employer_AddAsync_GetAll_ReturnsEmployer()
    {
        using var ctx = NewContext();
        var repo = new EmployerRepository(ctx);
        var employer = new Employer { EmployerCode = "ABC123", CompanyName = "Acme", RegistrationNumber = "REG123" };
        await repo.AddAsync(employer);
        await ctx.SaveChangesAsync();

        var all = await repo.GetAllAsync();
        Assert.That(all, Has.Count.EqualTo(1));
        Assert.That(all[0].EmployerCode, Is.EqualTo("ABC123"));
    }

    [Test]
    public async Task Employer_ExistsByCode_And_ByRegNo()
    {
        using var ctx = NewContext();
        var repo = new EmployerRepository(ctx);
        var employer = new Employer { EmployerCode = "ABC123", CompanyName = "Acme", RegistrationNumber = "REG123" };
        await repo.AddAsync(employer);
        await ctx.SaveChangesAsync();

        Assert.That(await repo.ExistsByEmployerCodeAsync("ABC123"), Is.True);
        Assert.That(await repo.ExistsByRegistrationNumberAsync("REG123"), Is.True);
        Assert.That(await repo.ExistsByEmployerCodeAsync("XYZ999"), Is.False);
    }

    // ---------- FundSchemeRepository ----------

    [Test]
    public async Task FundScheme_AddAsync_FindById_ReturnsScheme()
    {
        using var ctx = NewContext();
        var repo = new FundSchemeRepository(ctx);
        var scheme = new FundScheme
        {
            SchemeName = "EPF",
            SchemeType = SchemeType.EPF,
            EmployeeContributionRate = 12m,
            EmployerContributionRate = 12m,
            InterestRatePA = 8.15m
        };
        await repo.AddAsync(scheme);
        await ctx.SaveChangesAsync();

        var found = await repo.FindByIdAsync(scheme.SchemeId);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.SchemeName, Is.EqualTo("EPF"));
    }

    [Test]
    public async Task FundScheme_GetAll_ReturnsAllSchemes()
    {
        using var ctx = NewContext();
        var repo = new FundSchemeRepository(ctx);
        await repo.AddAsync(new FundScheme { SchemeName = "EPF", SchemeType = SchemeType.EPF });
        await repo.AddAsync(new FundScheme { SchemeName = "Gratuity", SchemeType = SchemeType.Gratuity });
        await ctx.SaveChangesAsync();

        var all = await repo.GetAllAsync();
        Assert.That(all, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task FundScheme_GetFirst_ReturnsFirstOrNull()
    {
        using var ctx = NewContext();
        var repo = new FundSchemeRepository(ctx);
        var first = await repo.GetFirstAsync();
        Assert.That(first, Is.Null);

        await repo.AddAsync(new FundScheme { SchemeName = "EPF", SchemeType = SchemeType.EPF });
        await ctx.SaveChangesAsync();

        first = await repo.GetFirstAsync();
        Assert.That(first, Is.Not.Null);
        Assert.That(first!.SchemeName, Is.EqualTo("EPF"));
    }
}
