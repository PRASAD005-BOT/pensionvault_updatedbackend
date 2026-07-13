using Microsoft.AspNetCore.Http;
using Members.Domain.Repositories;
using PensionVault.Shared.Http;

namespace Members.Services.ProxyRepositories;

public class HttpContributionRepository : BaseHttpRepository, IContributionRepository
{
    public HttpContributionRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public async Task<List<ExternalContribution>> GetByMemberAsync(Guid memberId)
        => await GetAsync<List<ExternalContribution>>($"api/remittances/member/{memberId}") ?? new List<ExternalContribution>();
}





