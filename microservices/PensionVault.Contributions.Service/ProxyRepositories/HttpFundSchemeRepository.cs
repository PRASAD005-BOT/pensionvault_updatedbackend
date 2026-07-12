using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Contributions.Service.ProxyRepositories;

public class HttpFundSchemeRepository : BaseHttpRepository, IFundSchemeRepository
{
    public HttpFundSchemeRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public Task<FundScheme?> FindByIdAsync(Guid schemeId)
        => GetAsync<FundScheme>($"api/schemes/{schemeId}");

    public async Task<List<FundScheme>> GetAllAsync()
        => await GetAsync<List<FundScheme>>("api/schemes") ?? new List<FundScheme>();

    public Task<FundScheme?> GetFirstAsync() => throw new NotSupportedException();
    public Task AddAsync(FundScheme scheme) => throw new NotSupportedException();
}

