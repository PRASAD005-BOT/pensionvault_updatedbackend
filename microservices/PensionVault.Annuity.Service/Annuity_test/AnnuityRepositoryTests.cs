using Annuity.Data;
using Annuity.Data.Repositories;
using Annuity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Annuity_test;

/// <summary>
/// Repository tests for <see cref="AnnuityRepository"/> using the EF Core in-memory
/// provider (a fresh, isolated database per test).
/// </summary>
[TestFixture]
public class AnnuityRepositoryTests
{
    private static AnnuityDbContext NewContext()
        => new(new DbContextOptionsBuilder<AnnuityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AnnuityPlan NewPlan(Guid? memberId = null)
        => new()
        {
            MemberId = memberId ?? Guid.NewGuid(),
            PlanType = AnnuityPlanType.LifeAnnuity,
            PurchaseValue = 100000m,
            MonthlyPension = 500m,
            AnnuityStartDate = DateTime.UtcNow,
            Status = AnnuityStatus.Active
        };

    [Test]
    public async Task AddAsync_ThenFindById_ReturnsPersistedPlan()
    {
        using var ctx = NewContext();
        var repo = new AnnuityRepository(ctx);
        var plan = NewPlan();

        await repo.AddAsync(plan);
        await ctx.SaveChangesAsync();

        var found = await repo.FindByIdAsync(plan.AnnuityId);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.MemberId, Is.EqualTo(plan.MemberId));
        Assert.That(found.MonthlyPension, Is.EqualTo(500m));
    }

    [Test]
    public async Task FindById_ReturnsNull_WhenMissing()
    {
        using var ctx = NewContext();
        var repo = new AnnuityRepository(ctx);

        var found = await repo.FindByIdAsync(Guid.NewGuid());
        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task ExistsDisbursementForMonth_ReflectsData()
    {
        using var ctx = NewContext();
        var repo = new AnnuityRepository(ctx);
        var plan = NewPlan();
        await repo.AddAsync(plan);
        await repo.AddDisbursementAsync(new MonthlyPensionDisbursement
        {
            AnnuityId = plan.AnnuityId,
            MemberId = plan.MemberId,
            Month = 5,
            Year = 2026,
            GrossAmount = 500m,
            NetAmount = 500m,
            Status = PensionDisbursementStatus.Disbursed
        });
        await ctx.SaveChangesAsync();

        Assert.That(await repo.ExistsDisbursementForMonthAsync(plan.AnnuityId, 5, 2026), Is.True);
        Assert.That(await repo.ExistsDisbursementForMonthAsync(plan.AnnuityId, 6, 2026), Is.False);
    }

    [Test]
    public async Task GetDisbursements_ReturnsOnlyForGivenAnnuity()
    {
        using var ctx = NewContext();
        var repo = new AnnuityRepository(ctx);
        var plan = NewPlan();
        var other = NewPlan();
        await repo.AddAsync(plan);
        await repo.AddAsync(other);
        await repo.AddDisbursementAsync(new MonthlyPensionDisbursement
        { AnnuityId = plan.AnnuityId, MemberId = plan.MemberId, Month = 1, Year = 2026 });
        await repo.AddDisbursementAsync(new MonthlyPensionDisbursement
        { AnnuityId = other.AnnuityId, MemberId = other.MemberId, Month = 1, Year = 2026 });
        await ctx.SaveChangesAsync();

        var list = await repo.GetDisbursementsAsync(plan.AnnuityId);
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0].AnnuityId, Is.EqualTo(plan.AnnuityId));
    }

    [Test]
    public async Task GetAll_ReturnsAllPlans()
    {
        using var ctx = NewContext();
        var repo = new AnnuityRepository(ctx);
        await repo.AddAsync(NewPlan());
        await repo.AddAsync(NewPlan());
        await ctx.SaveChangesAsync();

        var all = await repo.GetAllAsync();
        Assert.That(all, Has.Count.EqualTo(2));
    }
}
