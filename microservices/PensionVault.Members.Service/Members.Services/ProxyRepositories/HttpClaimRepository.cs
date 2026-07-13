using Microsoft.AspNetCore.Http;
using Members.Domain.Repositories;
using PensionVault.Shared.Http;

namespace Members.Services.ProxyRepositories;

public class HttpClaimRepository : BaseHttpRepository, IClaimRepository
{
    public HttpClaimRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public async Task<List<ExternalClaim>> GetAllAsync()
        => await GetAsync<List<ExternalClaim>>("api/claims") ?? new List<ExternalClaim>();
}





