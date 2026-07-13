using Claims.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Claims.Domain.Entities;
using Claims.Data;

namespace Claims.Data.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ClaimsDbContext _context;
    public ClaimRepository(ClaimsDbContext context) => _context = context;

    public Task<BenefitClaim?> FindByIdAsync(Guid claimId)
        => _context.BenefitClaims
            .Include(c => c.Disbursements)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

    public Task<List<BenefitClaim>> GetAllAsync()
        => _context.BenefitClaims
            .Include(c => c.Disbursements)
            .OrderByDescending(c => c.ClaimDate)
            .ToListAsync();

    public async Task AddAsync(BenefitClaim claim)
        => await _context.BenefitClaims.AddAsync(claim);

    public async Task AddDisbursementAsync(ClaimDisbursement disbursement)
        => await _context.ClaimDisbursements.AddAsync(disbursement);
}




