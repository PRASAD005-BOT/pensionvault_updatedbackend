using Microsoft.AspNetCore.Http;
using Members.Domain.Repositories;
using PensionVault.Shared.Http;

namespace Members.Services.ProxyRepositories;

public class HttpLedgerRepository : BaseHttpRepository, ILedgerRepository
{
    public HttpLedgerRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public async Task<List<ExternalLedgerEntry>> GetByAccountAsync(Guid accountId)
        => await GetAsync<List<ExternalLedgerEntry>>($"api/ledger/account/{accountId}") ?? new List<ExternalLedgerEntry>();
}





