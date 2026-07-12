using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Claims.Service.ProxyRepositories;

public class HttpLedgerRepository : BaseHttpRepository, ILedgerRepository
{
    public HttpLedgerRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public Task AddEntryAsync(LedgerEntry entry)
        => PostAsync("api/ledger", entry);

    public Task<List<LedgerEntry>> GetByAccountAsync(Guid accountId)
        => GetAsync<List<LedgerEntry>>($"api/ledger/account/{accountId}");

    public Task<List<LedgerEntry>> GetAllAsync() => throw new NotSupportedException();
    public Task<decimal> SumByTypeAsync(Guid accountId, EntryType entryType) => throw new NotSupportedException();
    public Task<bool> InterestAlreadyCreditedAsync(Guid accountId, string financialYear) => throw new NotSupportedException();
    public Task<List<InterestCreditRecord>> GetInterestRecordsAsync(Guid accountId) => throw new NotSupportedException();
    public Task AddInterestRecordAsync(InterestCreditRecord record) => throw new NotSupportedException();
}

