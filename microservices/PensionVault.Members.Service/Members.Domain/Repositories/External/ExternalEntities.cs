using Members.Domain.Entities;
namespace Members.Domain.Repositories;

public record ExternalClaim(
    Guid ClaimId,
    Guid MemberId,
    string ClaimType,
    DateTime ClaimDate,
    decimal EligibleAmount,
    decimal VestedAmount,
    decimal TaxDeductible,
    string Status
);

public record ExternalContribution(
    Guid ContributionId,
    Guid MemberId,
    string Period,
    decimal EmployeeAmount,
    decimal EmployerAmount,
    decimal TotalAmount,
    DateTime PostedDate,
    string Status
);

public record ExternalFundAccount(
    Guid AccountId,
    Guid MemberId,
    Guid SchemeId,
    DateTime AccountOpenDate,
    decimal EmployeeContributionBalance,
    decimal EmployerContributionBalance,
    decimal PensionBalance,
    decimal InterestAccrued,
    decimal TotalBalance,
    decimal VestingPercent,
    string Status
);

public record ExternalLedgerEntry(
    Guid EntryId,
    Guid AccountId,
    string EntryType,
    decimal Amount,
    decimal BalanceAfter,
    Guid? ReferenceId,
    DateTime EntryDate,
    string Status
);


