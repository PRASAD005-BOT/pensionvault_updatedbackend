using PensionVault.Domain.Entities;

namespace PensionVault.Domain.Interfaces;

public interface IAnnuityRepository
{
    Task<AnnuityPlan?> FindByIdAsync(Guid annuityId);
    Task<List<AnnuityPlan>> GetAllAsync();
    Task<List<MonthlyPensionDisbursement>> GetDisbursementsAsync(Guid annuityId);
    Task<MonthlyPensionDisbursement?> FindDisbursementByIdAsync(Guid disbursementId);
    Task<bool> ExistsDisbursementForMonthAsync(Guid annuityId, int month, int year);
    Task AddAsync(AnnuityPlan plan);
    Task AddDisbursementAsync(MonthlyPensionDisbursement disbursement);
}
