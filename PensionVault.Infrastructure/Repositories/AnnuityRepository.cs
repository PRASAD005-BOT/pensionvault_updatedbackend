using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Repositories;

public class AnnuityRepository : IAnnuityRepository
{
    private readonly AppDbContext _context;
    public AnnuityRepository(AppDbContext context) => _context = context;

    public Task<AnnuityPlan?> FindByIdAsync(Guid annuityId)
        => _context.AnnuityPlans
            .Include(a => a.Member)
            .FirstOrDefaultAsync(a => a.AnnuityId == annuityId);

    public Task<List<AnnuityPlan>> GetAllAsync()
        => _context.AnnuityPlans
            .Include(a => a.Member)
            .OrderByDescending(a => a.AnnuityStartDate)
            .ToListAsync();

    public Task<List<MonthlyPensionDisbursement>> GetDisbursementsAsync(Guid annuityId)
        => _context.MonthlyPensionDisbursements
            .Include(d => d.Member)
            .Where(d => d.AnnuityId == annuityId)
            .OrderByDescending(d => d.Year).ThenByDescending(d => d.Month)
            .ToListAsync();

    public Task<MonthlyPensionDisbursement?> FindDisbursementByIdAsync(Guid disbursementId)
        => _context.MonthlyPensionDisbursements
            .Include(d => d.Member)
            .FirstOrDefaultAsync(d => d.DisbursementId == disbursementId);

    public async Task AddAsync(AnnuityPlan plan)
        => await _context.AnnuityPlans.AddAsync(plan);

    public async Task AddDisbursementAsync(MonthlyPensionDisbursement disbursement)
        => await _context.MonthlyPensionDisbursements.AddAsync(disbursement);
}
