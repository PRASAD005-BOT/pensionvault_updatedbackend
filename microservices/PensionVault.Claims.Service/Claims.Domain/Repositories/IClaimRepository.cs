using Claims.Domain.Entities;

namespace Claims.Domain.Repositories;

public interface IClaimRepository
{
    Task<BenefitClaim?> FindByIdAsync(Guid claimId);
    Task<List<BenefitClaim>> GetAllAsync();
    Task AddAsync(BenefitClaim claim);
    Task AddDisbursementAsync(ClaimDisbursement disbursement);
}


