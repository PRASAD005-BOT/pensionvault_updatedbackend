using Claims.Data;
using Claims.Data.Repositories;
using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Claims_test;

/// <summary>
/// Repository tests for <see cref="ClaimRepository"/> using the EF Core in-memory provider.
/// </summary>
[TestFixture]
public class ClaimRepositoryTests
{
    private static ClaimsDbContext NewContext()
        => new(new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static BenefitClaim NewClaim(Guid memberId, ClaimStatus status, decimal amount = 1000m,
        ClaimType type = ClaimType.Retirement, DateTime? claimDate = null)
        => new()
        {
            MemberId = memberId,
            ClaimType = type,
            ClaimDate = claimDate ?? DateTime.UtcNow,
            EligibleAmount = amount,
            VestedAmount = amount,
            TaxDeductible = 0m,
            Status = status,
            Description = "A valid claim description here."
        };

    [Test]
    public async Task AddAsync_ThenFindById_ReturnsClaimWithDisbursements()
    {
        using var ctx = NewContext();
        var repo = new ClaimRepository(ctx);
        var claim = NewClaim(Guid.NewGuid(), ClaimStatus.Approved);
        await repo.AddAsync(claim);
        await repo.AddDisbursementAsync(new ClaimDisbursement
        {
            ClaimId = claim.ClaimId,
            MemberId = claim.MemberId,
            DisbursedAmount = 900m,
            NetAmount = 900m,
            Status = DisbursementStatus.Processed
        });
        await ctx.SaveChangesAsync();

        var found = await repo.FindByIdAsync(claim.ClaimId);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Disbursements, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task HasRecentDuplicate_DetectsIdenticalRecentClaim()
    {
        using var ctx = NewContext();
        var repo = new ClaimRepository(ctx);
        var memberId = Guid.NewGuid();
        await repo.AddAsync(NewClaim(memberId, ClaimStatus.Submitted, 1000m, ClaimType.Retirement, DateTime.UtcNow));
        await ctx.SaveChangesAsync();

        var since = DateTime.UtcNow.AddSeconds(-10);
        Assert.That(await repo.HasRecentDuplicateAsync(memberId, ClaimType.Retirement, 1000m, since), Is.True);
        Assert.That(await repo.HasRecentDuplicateAsync(memberId, ClaimType.Retirement, 9999m, since), Is.False);
    }

    [Test]
    public async Task GetActiveClaimsTotal_SumsOnlyInFlightClaims()
    {
        using var ctx = NewContext();
        var repo = new ClaimRepository(ctx);
        var memberId = Guid.NewGuid();
        await repo.AddAsync(NewClaim(memberId, ClaimStatus.Submitted, 1000m));
        await repo.AddAsync(NewClaim(memberId, ClaimStatus.UnderReview, 2000m));
        await repo.AddAsync(NewClaim(memberId, ClaimStatus.Approved, 3000m));
        await repo.AddAsync(NewClaim(memberId, ClaimStatus.Disbursed, 5000m));   // excluded
        await repo.AddAsync(NewClaim(memberId, ClaimStatus.Rejected, 7000m));    // excluded
        await ctx.SaveChangesAsync();

        var total = await repo.GetActiveClaimsTotalAsync(memberId);
        Assert.That(total, Is.EqualTo(6000m));   // 1000 + 2000 + 3000
    }

    [Test]
    public async Task GetAll_ReturnsClaimsNewestFirst()
    {
        using var ctx = NewContext();
        var repo = new ClaimRepository(ctx);
        var memberId = Guid.NewGuid();
        await repo.AddAsync(NewClaim(memberId, ClaimStatus.Submitted, 1000m, ClaimType.Retirement, DateTime.UtcNow.AddDays(-2)));
        await repo.AddAsync(NewClaim(memberId, ClaimStatus.Submitted, 2000m, ClaimType.Retirement, DateTime.UtcNow));
        await ctx.SaveChangesAsync();

        var all = await repo.GetAllAsync();
        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(all[0].EligibleAmount, Is.EqualTo(2000m));   // newest first
    }
}
