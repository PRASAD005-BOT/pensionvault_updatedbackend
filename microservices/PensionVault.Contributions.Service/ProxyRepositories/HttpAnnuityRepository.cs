using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Contributions.Service.ProxyRepositories;

public class HttpAnnuityRepository : BaseHttpRepository, IAnnuityRepository
{
    public HttpAnnuityRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public Task<AnnuityPlan?> FindByIdAsync(Guid annuityId) => throw new NotSupportedException();
    public Task<List<AnnuityPlan>> GetAllAsync() => throw new NotSupportedException();
    public Task<List<MonthlyPensionDisbursement>> GetDisbursementsAsync(Guid annuityId) => throw new NotSupportedException();
    public Task<MonthlyPensionDisbursement?> FindDisbursementByIdAsync(Guid disbursementId) => throw new NotSupportedException();
    public Task<bool> ExistsDisbursementForMonthAsync(Guid annuityId, int month, int year) => throw new NotSupportedException();
    public Task AddAsync(AnnuityPlan plan) => throw new NotSupportedException();
    public Task AddDisbursementAsync(MonthlyPensionDisbursement disbursement) => throw new NotSupportedException();
}

