using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Repositories;

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
            .Where(e => e.AccountId == accountId && e.EntryType == entryType)
            .SumAsync(e => e.Amount);

    public Task<bool> InterestAlreadyCreditedAsync(Guid accountId, string financialYear) 
        => _context.InterestCreditRecords
            .AnyAsync(r => r.AccountId == accountId && r.FinancialYear == financialYear);

    public Task<List<InterestCreditRecord>> GetInterestRecordsAsync(Guid accountId)
        => _context.InterestCreditRecords
            .Where(r => r.AccountId == accountId)
            .OrderByDescending(r => r.FinancialYear)
            .ToListAsync();

    public async Task AddEntryAsync(LedgerEntry entry)
        => await _context.LedgerEntries.AddAsync(entry);

    public async Task AddInterestRecordAsync(InterestCreditRecord record)
        => await _context.InterestCreditRecords.AddAsync(record);
}
