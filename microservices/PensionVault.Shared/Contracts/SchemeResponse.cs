namespace PensionVault.Shared.Contracts;

public record SchemeResponse(
    Guid SchemeId,
    string SchemeName,
    string SchemeType,
    decimal EmployeeContributionRate,
    decimal EmployerContributionRate,
    decimal InterestRatePA,
    string? VestingSchedule,
    string Status,
    string? Description
);


