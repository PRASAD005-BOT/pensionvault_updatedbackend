using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Repositories;

public class AnnuityRepository : IAnnuityRepository
{
    private readonly AnnuityDbContext _context;
    public AnnuityRepository(AnnuityDbContext context) => _context = context;

    public Task<AnnuityPlan?> FindByIdAsync(Guid annuityId)
        => _context.AnnuityPlans
            .FirstOrDefaultAsync(a => a.AnnuityId == annuityId);

    public Task<List<AnnuityPlan>> GetAllAsync()
        => _context.AnnuityPlans
            .OrderByDescending(a => a.AnnuityStartDate)
            .ToListAsync();

    public Task<List<MonthlyPensionDisbursement>> GetDisbursementsAsync(Guid annuityId)
        => _context.MonthlyPensionDisbursements
            .Where(d => d.AnnuityId == annuityId)
            .OrderByDescending(d => d.Year).ThenByDescending(d => d.Month)
            .ToListAsync();

    public Task<MonthlyPensionDisbursement?> FindDisbursementByIdAsync(Guid disbursementId)
        => _context.MonthlyPensionDisbursements
            .FirstOrDefaultAsync(d => d.DisbursementId == disbursementId);

    public Task<bool> ExistsDisbursementForMonthAsync(Guid annuityId, int month, int year)
        => _context.MonthlyPensionDisbursements
            .AnyAsync(d => d.AnnuityId == annuityId && d.Month == month && d.Year == year);

    public async Task AddAsync(AnnuityPlan plan)
        => await _context.AnnuityPlans.AddAsync(plan);

    public async Task AddDisbursementAsync(MonthlyPensionDisbursement disbursement)
        => await _context.MonthlyPensionDisbursements.AddAsync(disbursement);
}
