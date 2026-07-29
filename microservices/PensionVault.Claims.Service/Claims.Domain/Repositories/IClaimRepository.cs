using Claims.Domain.Entities;

namespace Claims.Domain.Repositories;

public interface IClaimRepository
{
    Task<BenefitClaim?> FindByIdAsync(Guid claimId);
    Task<List<BenefitClaim>> GetAllAsync();
    Task AddAsync(BenefitClaim claim);
    Task AddDisbursementAsync(ClaimDisbursement disbursement);

    /// <summary>
    /// Returns true when an identical claim (same member, type and amount) was
    /// created on or after <paramref name="since"/>. Used to reject accidental
    /// duplicate submissions from a rapid double-click.
    /// </summary>
    Task<bool> HasRecentDuplicateAsync(Guid memberId, ClaimType claimType, decimal eligibleAmount, DateTime since);
}


