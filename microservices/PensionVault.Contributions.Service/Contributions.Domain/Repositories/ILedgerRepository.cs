using Contributions.Domain.Entities;

namespace Contributions.Domain.Repositories;

public interface ILedgerRepository
{
    Task<List<LedgerEntry>> GetByAccountAsync(Guid accountId);
    Task<List<LedgerEntry>> GetAllAsync();
    Task<decimal> SumByTypeAsync(Guid accountId, EntryType entryType);
    Task<bool> InterestAlreadyCreditedAsync(Guid accountId, string financialYear);
    Task<List<InterestCreditRecord>> GetInterestRecordsAsync(Guid accountId);
    Task AddEntryAsync(LedgerEntry entry);
    Task AddInterestRecordAsync(InterestCreditRecord record);
}


