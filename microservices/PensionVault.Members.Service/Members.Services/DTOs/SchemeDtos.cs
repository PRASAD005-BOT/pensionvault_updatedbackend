using Members.Domain.Entities;

namespace Members.Services.DTOs;

public record CreateSchemeRequest(
    string SchemeName,
    SchemeType SchemeType,
    decimal EmployeeContributionRate,
    decimal EmployerContributionRate,
    decimal InterestRatePA,
    int VestingYears,
    decimal VestingPercent,
    string? Description
);

public record UpdateSchemeRequest(
    string SchemeName,
    decimal EmployeeContributionRate,
    decimal EmployerContributionRate,
    decimal InterestRatePA,
    int VestingYears,
    decimal VestingPercent,
    SchemeStatus Status,
    string? Description
);


