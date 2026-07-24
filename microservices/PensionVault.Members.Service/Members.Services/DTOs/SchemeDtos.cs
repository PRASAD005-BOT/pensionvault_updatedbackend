using Members.Domain.Entities;

namespace Members.Services.DTOs;

public record CreateSchemeRequest(
    string SchemeName,
    SchemeType SchemeType,
    decimal EmployeeContributionRate,
    decimal EmployerContributionRate,
    decimal InterestRatePA,
    string? VestingSchedule
);

public record UpdateSchemeRequest(
    string SchemeName,
    decimal EmployeeContributionRate,
    decimal EmployerContributionRate,
    decimal InterestRatePA,
    string? VestingSchedule,
    SchemeStatus Status
);


