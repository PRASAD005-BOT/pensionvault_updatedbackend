namespace PensionVault.Shared.Contracts;

public record FundAccountResponse(
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


