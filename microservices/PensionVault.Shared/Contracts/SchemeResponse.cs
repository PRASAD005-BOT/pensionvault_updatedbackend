namespace PensionVault.Shared.Contracts;

public record SchemeResponse(
    Guid SchemeId,
    string SchemeName,
    string SchemeType,
    decimal EmployeeContributionRate,
    decimal EmployerContributionRate,
    decimal InterestRatePA,
    int VestingYears,
    decimal VestingPercent,
    string Status,
    string Description
);


