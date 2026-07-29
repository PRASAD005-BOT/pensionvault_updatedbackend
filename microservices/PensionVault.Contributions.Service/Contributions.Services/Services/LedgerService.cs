using Contributions.Services.DTOs;
using Contributions.Domain.Entities;
using Contributions.Domain.Repositories;
using Contributions.Services.HttpClients;

namespace Contributions.Services;

public class LedgerService : ILedgerService
{
    private readonly ILedgerRepository _ledgerRepo;
    private readonly IFundAccountRepository _accountRepo;
    private readonly MemberServiceClient _memberClient;
    private readonly IUnitOfWork _unitOfWork;

    public LedgerService(
        ILedgerRepository ledgerRepo,
        IFundAccountRepository accountRepo,
        MemberServiceClient memberClient,
        IUnitOfWork unitOfWork)
    {
        _ledgerRepo = ledgerRepo;
        _accountRepo = accountRepo;
        _memberClient = memberClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<LedgerEntryResponse>> GetAccountLedgerAsync(Guid accountId)
    {
        var entries = await _ledgerRepo.GetByAccountAsync(accountId);
        return await BuildResponsesAsync(entries);
    }

    public async Task<IEnumerable<LedgerEntryResponse>> GetAllLedgerEntriesAsync()
    {
        var entries = await _ledgerRepo.GetAllAsync();
        return await BuildResponsesAsync(entries);
    }

    // Resolves the member name behind each ledger entry (account -> member),
    // caching account and member lookups so each is fetched at most once.
    private async Task<List<LedgerEntryResponse>> BuildResponsesAsync(List<LedgerEntry> entries)
    {
        var accountToMember = new Dictionary<Guid, Guid>();
        var memberNames = new Dictionary<Guid, string>();

        foreach (var accountId in entries.Select(e => e.AccountId).Distinct())
        {
            var account = await _accountRepo.FindByIdAsync(accountId);
            if (account == null) continue;
            accountToMember[accountId] = account.MemberId;

            if (!memberNames.ContainsKey(account.MemberId))
            {
                var member = await _memberClient.GetMemberByIdAsync(account.MemberId);
                memberNames[account.MemberId] = member?.Name ?? "";
            }
        }

        return entries.Select(e =>
        {
            Guid? memberId = accountToMember.TryGetValue(e.AccountId, out var mId) ? mId : null;
            var name = memberId.HasValue && memberNames.TryGetValue(memberId.Value, out var n) ? n : "";
            return new LedgerEntryResponse(
                e.EntryId, e.AccountId, e.EntryType, e.Amount,
                e.BalanceAfter, e.EntryDate, e.ReferenceId, e.Status, memberId, name);
        }).ToList();
    }

    public async Task<InterestCreditResponse> CreditInterestAsync(CreditInterestRequest request)
    {
        if (request.AccountId == Guid.Empty)
            throw new ArgumentException("Please enter a valid Account ID.");

        if (request.InterestRate <= 0)
            throw new ArgumentException("Interest rate must be greater than zero.");

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

        int century = (startYear / 100) * 100;
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


