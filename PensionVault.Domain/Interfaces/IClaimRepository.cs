using PensionVault.Domain.Entities;

namespace PensionVault.Domain.Interfaces;

public interface IClaimRepository
{
    Task<BenefitClaim?> FindByIdAsync(Guid claimId);
    Task<List<BenefitClaim>> GetAllAsync();
    Task AddAsync(BenefitClaim claim);
    Task AddDisbursementAsync(ClaimDisbursement disbursement);
}
