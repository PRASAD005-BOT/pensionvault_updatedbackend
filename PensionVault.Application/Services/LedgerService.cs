using PensionVault.Application.DTOs.Ledger;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Application.Services;

public class LedgerService : ILedgerService
{
    private readonly ILedgerRepository _ledgerRepo;
    private readonly IFundAccountRepository _accountRepo;
    private readonly IUnitOfWork _unitOfWork;

    public LedgerService(
        ILedgerRepository ledgerRepo,
        IFundAccountRepository accountRepo,
        IUnitOfWork unitOfWork)
    {
        _ledgerRepo = ledgerRepo;
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<LedgerEntryResponse>> GetAccountLedgerAsync(Guid accountId)
    {
        var entries = await _ledgerRepo.GetByAccountAsync(accountId);
        return entries.Select(e => new LedgerEntryResponse(
            e.EntryId, e.AccountId, e.EntryType, e.Amount,
            e.BalanceAfter, e.EntryDate, e.ReferenceId, e.Status));
    }

    public async Task<IEnumerable<LedgerEntryResponse>> GetAllLedgerEntriesAsync()
    {
        var entries = await _ledgerRepo.GetAllAsync();
        return entries.Select(e => new LedgerEntryResponse(
            e.EntryId, e.AccountId, e.EntryType, e.Amount,
            e.BalanceAfter, e.EntryDate, e.ReferenceId, e.Status));
    }

    public async Task<InterestCreditResponse> CreditInterestAsync(CreditInterestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FinancialYear) || request.FinancialYear.Length != 7)
        {
            throw new ArgumentException("Financial year must be in the format YYYY-YY (e.g., 2025-26).");
        }

        var parts = request.FinancialYear.Split('-');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out int startYear) ||
            !int.TryParse(parts[1], out int endYearValue))
        {
            throw new ArgumentException("Financial year must be in the format YYYY-YY (e.g., 2025-26).");
        }

        int century = (startYear / 100) * 100;  // 2000
        int expectedEndYear = startYear + 1;
        int providedEndYear = century + endYearValue;

        if (providedEndYear != expectedEndYear)
        {
            throw new ArgumentException($"Invalid span. Financial year '{request.FinancialYear}' must cover exactly 1 consecutive year.");
        }

        var account = await _accountRepo.FindByIdAsync(request.AccountId)
            ?? throw new KeyNotFoundException("Fund account not found.");


        if (await _ledgerRepo.InterestAlreadyCreditedAsync(request.AccountId, request.FinancialYear))
            throw new InvalidOperationException($"Interest already credited for {request.FinancialYear}.");

        var totalContributions = await _ledgerRepo.SumByTypeAsync(request.AccountId, EntryType.ContributionCredit);
        var openingBalance = account.TotalBalance - totalContributions;
        var interestAmount = Math.Round(
            (openingBalance + totalContributions / 2) * (request.InterestRate / 100), 2);

        var record = new InterestCreditRecord
        {
            AccountId = request.AccountId,
            FinancialYear = request.FinancialYear,
            OpeningBalance = openingBalance,
            TotalContributions = totalContributions,
            InterestRateApplied = request.InterestRate,
            InterestAmount = interestAmount,
            ClosingBalance = account.TotalBalance + interestAmount,
            CreditedDate = DateTime.UtcNow,
            Status = InterestCreditStatus.Credited
        };
        await _ledgerRepo.AddInterestRecordAsync(record);

        account.InterestAccrued += interestAmount;
        account.TotalBalance += interestAmount;

        await _ledgerRepo.AddEntryAsync(new LedgerEntry
        {
            AccountId = account.AccountId,
            EntryType = EntryType.InterestCredit,
            Amount = interestAmount,
            BalanceAfter = account.TotalBalance,
            ReferenceId = record.InterestId.ToString(),
            Status = LedgerEntryStatus.Posted
        });

        await _unitOfWork.SaveChangesAsync();
        return new InterestCreditResponse(
            record.InterestId, record.AccountId, record.FinancialYear,
            record.OpeningBalance, record.TotalContributions, record.InterestRateApplied,
            record.InterestAmount, record.ClosingBalance, record.CreditedDate, record.Status);
    }

    public async Task<IEnumerable<InterestCreditResponse>> GetInterestRecordsAsync(Guid accountId)
    {
        var records = await _ledgerRepo.GetInterestRecordsAsync(accountId);
        return records.Select(r => new InterestCreditResponse(
            r.InterestId, r.AccountId, r.FinancialYear,
            r.OpeningBalance, r.TotalContributions, r.InterestRateApplied,
            r.InterestAmount, r.ClosingBalance, r.CreditedDate, r.Status));
    }
}
