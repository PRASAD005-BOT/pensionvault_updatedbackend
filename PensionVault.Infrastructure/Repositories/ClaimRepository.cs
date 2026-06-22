using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly AppDbContext _context;
    public ClaimRepository(AppDbContext context) => _context = context;

    public Task<BenefitClaim?> FindByIdAsync(Guid claimId)
        => _context.BenefitClaims
            .Include(c => c.Member)
            .Include(c => c.ProcessedBy)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

    public Task<List<BenefitClaim>> GetAllAsync()
        => _context.BenefitClaims
            .Include(c => c.Member)
            .Include(c => c.ProcessedBy)
            .OrderByDescending(c => c.ClaimDate)
            .ToListAsync();

    public async Task AddAsync(BenefitClaim claim)
        => await _context.BenefitClaims.AddAsync(claim);

    public async Task AddDisbursementAsync(ClaimDisbursement disbursement)
        => await _context.ClaimDisbursements.AddAsync(disbursement);
}
