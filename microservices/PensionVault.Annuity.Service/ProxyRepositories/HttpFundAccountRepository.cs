using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Annuity.Service.ProxyRepositories;

public class HttpFundAccountRepository : BaseHttpRepository, IFundAccountRepository
{
    public HttpFundAccountRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public Task<FundAccount?> FindByIdAsync(Guid accountId)
        => GetAsync<FundAccount>($"api/fundaccounts/{accountId}");

    public Task<FundAccount?> FindActiveByMemberAsync(Guid memberId)
        => GetAsync<FundAccount>($"api/fundaccounts/active/member/{memberId}");

    public async Task<List<FundAccount>> GetByMemberAsync(Guid memberId)
        => await GetAsync<List<FundAccount>>($"api/fundaccounts/member/{memberId}") ?? new List<FundAccount>();

    public async Task<bool> ExistsByMemberAsync(Guid memberId)
        => await GetAsync<bool>($"api/fundaccounts/exists/member/{memberId}");

    public Task AddAsync(FundAccount account)
        => PostAsync("api/fundaccounts", account);
}

