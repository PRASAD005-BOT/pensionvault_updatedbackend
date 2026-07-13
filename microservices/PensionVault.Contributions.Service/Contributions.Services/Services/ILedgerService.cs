using Contributions.Domain.Repositories;
using Contributions.Services.DTOs;

namespace Contributions.Services;

public interface ILedgerService
{
    Task<IEnumerable<LedgerEntryResponse>> GetAccountLedgerAsync(Guid accountId);
    Task<IEnumerable<LedgerEntryResponse>> GetAllLedgerEntriesAsync();
    Task<InterestCreditResponse> CreditInterestAsync(CreditInterestRequest request);
    Task<IEnumerable<InterestCreditResponse>> GetInterestRecordsAsync(Guid accountId);
}



