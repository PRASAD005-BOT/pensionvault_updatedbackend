using Microsoft.AspNetCore.Http;
using Members.Domain.Repositories;
using PensionVault.Shared.Http;

namespace Members.Services.ProxyRepositories;

public class HttpFundAccountRepository : BaseHttpRepository, IFundAccountRepository
{
    public HttpFundAccountRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public async Task<List<ExternalFundAccount>> GetByMemberAsync(Guid memberId)
        => await GetAsync<List<ExternalFundAccount>>($"api/fundaccounts/member/{memberId}") ?? new List<ExternalFundAccount>();

    public Task AddAsync(ExternalFundAccount account)
        => PostAsync("api/fundaccounts", account);
}





