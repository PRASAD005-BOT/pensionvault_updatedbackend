using Contributions.Domain.Entities;

namespace Contributions.Services.DTOs;

public record LedgerEntryResponse(
    Guid EntryId,
    Guid AccountId,
    EntryType EntryType,
    decimal Amount,
    decimal BalanceAfter,
    DateTime EntryDate,
    string? ReferenceId,
    LedgerEntryStatus Status
);

public record CreditInterestRequest(
    Guid AccountId,
    string FinancialYear,
    decimal InterestRate
);

public record InterestCreditResponse(
    Guid InterestId,
    Guid AccountId,
    string FinancialYear,
    decimal OpeningBalance,
    decimal TotalContributions,
    decimal InterestRateApplied,
    decimal InterestAmount,
    decimal ClosingBalance,
    DateTime CreditedDate,
    InterestCreditStatus Status
);


