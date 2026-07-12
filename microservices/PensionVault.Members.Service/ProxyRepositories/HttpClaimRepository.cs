using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Members.Service.ProxyRepositories;

public class HttpClaimRepository : BaseHttpRepository, IClaimRepository
{
    public HttpClaimRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public async Task<List<BenefitClaim>> GetAllAsync()
        => await GetAsync<List<BenefitClaim>>("api/claims") ?? new List<BenefitClaim>();

    public Task<BenefitClaim?> FindByIdAsync(Guid claimId) => throw new NotSupportedException();
    public Task AddAsync(BenefitClaim claim) => throw new NotSupportedException();
    public Task AddDisbursementAsync(ClaimDisbursement disbursement) => throw new NotSupportedException();
}

