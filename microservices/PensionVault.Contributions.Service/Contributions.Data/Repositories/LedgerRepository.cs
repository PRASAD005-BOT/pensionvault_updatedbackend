using Contributions.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Contributions.Domain.Entities;
using Contributions.Data;

namespace Contributions.Data.Repositories;

public class LedgerRepository : ILedgerRepository
{
    private readonly ContributionsDbContext _context;
    public LedgerRepository(ContributionsDbContext context) => _context = context;

    public Task<List<LedgerEntry>> GetByAccountAsync(Guid accountId)
        => _context.LedgerEntries
            .Where(e => e.AccountId == accountId)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();

    public Task<List<LedgerEntry>> GetAllAsync()
        => _context.LedgerEntries
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();

    public Task<decimal> SumByTypeAsync(Guid accountId, EntryType entryType)
        => _context.LedgerEntries
            .Where(e => e.AccountId == accountId && e.EntryType == entryType && e.Status == LedgerEntryStatus.Posted)
            .SumAsync(e => e.Amount);

    public Task<bool> InterestAlreadyCreditedAsync(Guid accountId, string financialYear)
        => _context.InterestCreditRecords
            .AnyAsync(r => r.AccountId == accountId && r.FinancialYear == financialYear && r.Status == InterestCreditStatus.Credited);

    public Task<List<InterestCreditRecord>> GetInterestRecordsAsync(Guid accountId)
        => _context.InterestCreditRecords
            .Where(r => r.AccountId == accountId)
            .OrderByDescending(r => r.CreditedDate)
            .ToListAsync();

    public async Task AddEntryAsync(LedgerEntry entry)
        => await _context.LedgerEntries.AddAsync(entry);

    public async Task AddInterestRecordAsync(InterestCreditRecord record)
        => await _context.InterestCreditRecords.AddAsync(record);
}




