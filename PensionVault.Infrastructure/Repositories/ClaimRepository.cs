using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ClaimsDbContext _context;
    public ClaimRepository(ClaimsDbContext context) => _context = context;

    public Task<BenefitClaim?> FindByIdAsync(Guid claimId)
        => _context.BenefitClaims
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

    public Task<List<BenefitClaim>> GetAllAsync()
        => _context.BenefitClaims
            .OrderByDescending(c => c.ClaimDate)
            .ToListAsync();

    public async Task AddAsync(BenefitClaim claim)
        => await _context.BenefitClaims.AddAsync(claim);

    public async Task AddDisbursementAsync(ClaimDisbursement disbursement)
        => await _context.ClaimDisbursements.AddAsync(disbursement);
}
