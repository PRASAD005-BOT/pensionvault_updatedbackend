using Annuity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Annuity.Domain.Entities;
using Annuity.Data;

namespace Annuity.Data.Repositories;

public class AnnuityRepository : IAnnuityRepository
{
    private readonly AnnuityDbContext _context;
    public AnnuityRepository(AnnuityDbContext context) => _context = context;

    public Task<AnnuityPlan?> FindByIdAsync(Guid annuityId)
        => _context.AnnuityPlans
            .Include(a => a.PensionDisbursements)
            .FirstOrDefaultAsync(a => a.AnnuityId == annuityId);

    public Task<List<AnnuityPlan>> GetAllAsync()
        => _context.AnnuityPlans
            .Include(a => a.PensionDisbursements)
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




