using PensionVault.Domain.Enums;

namespace PensionVault.Application.DTOs.Contributions;

public record CreateRemittanceRequest(
    Guid EmployerId,
    string RemittancePeriod,
    decimal TotalEmployeeShare,
    decimal TotalEmployerShare,
    int CoverageCount,
    List<MemberContributionItem> MemberContributions
);

public record MemberContributionItem(
    Guid MemberId,
    decimal EmployeeAmount,
    decimal EmployerAmount
);

public record RemittanceResponse(
    Guid RemittanceId,
    Guid EmployerId,
    string EmployerName,
    string RemittancePeriod,
    decimal TotalEmployeeShare,
    decimal TotalEmployerShare,
    decimal TotalAmount,
    DateTime RemittanceDate,
    int CoverageCount,
    RemittanceStatus Status
);

public record MemberContributionResponse(
    Guid ContributionId,
    Guid MemberId,
    string MemberName,
    string Period,
    decimal EmployeeAmount,
    decimal EmployerAmount,
    decimal TotalAmount,
    DateTime PostedDate,
    ContributionStatus Status
);

public record ReconciliationReportResponse(
    Guid RemittanceId,
    string RemittancePeriod,
    int ExpectedCount,
    int ReconciledCount,
    decimal TotalExpectedAmount,
    decimal TotalReconciledAmount,
    string Status
);

public record DefaulterSummaryResponse(
    Guid EmployerId,
    string EmployerName,
    int MissingPeriods,
    decimal EstimatedShortfall,
    string LastRemittancePeriod
);
