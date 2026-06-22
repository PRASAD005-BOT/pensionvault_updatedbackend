using PensionVault.Application.DTOs.Ledger;

namespace PensionVault.Application.Interfaces;

public interface ILedgerService
{
    Task<IEnumerable<LedgerEntryResponse>> GetAccountLedgerAsync(Guid accountId);
    Task<IEnumerable<LedgerEntryResponse>> GetAllLedgerEntriesAsync();
    Task<InterestCreditResponse> CreditInterestAsync(CreditInterestRequest request);
    Task<IEnumerable<InterestCreditResponse>> GetInterestRecordsAsync(Guid accountId);
}
