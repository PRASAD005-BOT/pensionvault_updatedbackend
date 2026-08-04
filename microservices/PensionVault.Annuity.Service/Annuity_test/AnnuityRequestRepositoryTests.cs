using Annuity.Data;
using Annuity.Data.Repositories;
using Annuity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Annuity_test;

/// <summary>
/// Repository tests for <see cref="AnnuityRequestRepository"/> using the EF Core
/// in-memory provider.
/// </summary>
[TestFixture]
public class AnnuityRequestRepositoryTests
{
    private static AnnuityDbContext NewContext()
        => new(new DbContextOptionsBuilder<AnnuityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AnnuityRequest NewRequest(Guid memberId, AnnuityRequestStatus status)
        => new()
        {
            MemberId = memberId,
            PlanType = AnnuityPlanType.LifeAnnuity,
            PensionBalanceAtRequest = 100000m,
            EstimatedMonthly = 500m,
            Status = status,
            RequestedAt = DateTime.UtcNow
        };

    [Test]
    public async Task AddAsync_ThenFindById_ReturnsRequest()
    {
        using var ctx = NewContext();
        var repo = new AnnuityRequestRepository(ctx);
        var request = NewRequest(Guid.NewGuid(), AnnuityRequestStatus.Pending);

        await repo.AddAsync(request);
        await ctx.SaveChangesAsync();

        var found = await repo.FindByIdAsync(request.RequestId);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.MemberId, Is.EqualTo(request.MemberId));
    }

    [Test]
    public async Task GetPending_ReturnsOnlyPendingRequests()
    {
        using var ctx = NewContext();
        var repo = new AnnuityRequestRepository(ctx);
        await repo.AddAsync(NewRequest(Guid.NewGuid(), AnnuityRequestStatus.Pending));
        await repo.AddAsync(NewRequest(Guid.NewGuid(), AnnuityRequestStatus.Approved));
        await repo.AddAsync(NewRequest(Guid.NewGuid(), AnnuityRequestStatus.Rejected));
        await ctx.SaveChangesAsync();

        var pending = await repo.GetPendingAsync();
        Assert.That(pending, Has.Count.EqualTo(1));
        Assert.That(pending[0].Status, Is.EqualTo(AnnuityRequestStatus.Pending));
    }

    [Test]
    public async Task FindPendingByMember_ReturnsPendingForThatMember()
    {
        using var ctx = NewContext();
        var repo = new AnnuityRequestRepository(ctx);
        var memberId = Guid.NewGuid();
        await repo.AddAsync(NewRequest(memberId, AnnuityRequestStatus.Pending));
        await repo.AddAsync(NewRequest(Guid.NewGuid(), AnnuityRequestStatus.Pending));
        await ctx.SaveChangesAsync();

        var found = await repo.FindPendingByMemberAsync(memberId);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.MemberId, Is.EqualTo(memberId));
    }

    [Test]
    public async Task FindPendingByMember_ReturnsNull_WhenOnlyNonPending()
    {
        using var ctx = NewContext();
        var repo = new AnnuityRequestRepository(ctx);
        var memberId = Guid.NewGuid();
        await repo.AddAsync(NewRequest(memberId, AnnuityRequestStatus.Approved));
        await ctx.SaveChangesAsync();

        var found = await repo.FindPendingByMemberAsync(memberId);
        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task GetByMember_ReturnsAllForThatMember()
    {
        using var ctx = NewContext();
        var repo = new AnnuityRequestRepository(ctx);
        var memberId = Guid.NewGuid();
        await repo.AddAsync(NewRequest(memberId, AnnuityRequestStatus.Pending));
        await repo.AddAsync(NewRequest(memberId, AnnuityRequestStatus.Rejected));
        await repo.AddAsync(NewRequest(Guid.NewGuid(), AnnuityRequestStatus.Pending));
        await ctx.SaveChangesAsync();

        var list = await repo.GetByMemberAsync(memberId);
        Assert.That(list, Has.Count.EqualTo(2));
        Assert.That(list, Has.All.Matches<AnnuityRequest>(r => r.MemberId == memberId));
    }
}
