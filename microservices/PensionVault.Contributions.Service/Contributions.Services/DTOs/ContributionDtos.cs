using Contributions.Domain.Entities;

namespace Contributions.Services.DTOs;

public record CreateRemittanceRequest(
    Guid EmployerId,
    string RemittancePeriod,
    decimal TotalEmployeeShare,
    decimal TotalEmployerShare,
    decimal TotalPensionAmount,
    int CoverageCount,
    List<MemberContributionItem> MemberContributions
);

public record MemberContributionItem(
    Guid MemberId,
    decimal EmployeeAmount,
    decimal EmployerAmount,
    decimal PensionAmount
);

public record RemittanceResponse(
    Guid RemittanceId,
    Guid EmployerId,
    string EmployerName,
    string RemittancePeriod,
    decimal TotalEmployeeShare,
    decimal TotalEmployerShare,
    decimal TotalPensionAmount,
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
    decimal PensionAmount,
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

public record MemberShortfallResponse(
    Guid RemittanceId,
    string RemittancePeriod,
    string RemittanceStatus,
    string Message
);

public record CreateShortfallRequest(
    Guid ContributionId,
    string Reason
);

public record ResolveShortfallRequest(
    decimal? NewEmployeeAmount,
    decimal? NewEmployerAmount,
    decimal? NewPensionAmount,
    string ResolutionNote
);

public record RejectShortfallRequest(
    string RejectionNote
);

public record ShortfallRequestResponse(
    Guid ShortfallRequestId,
    Guid ContributionId,
    Guid MemberId,
    string MemberName,
    string Period,
    decimal EmployeeAmount,
    decimal EmployerAmount,
    decimal PensionAmount,
    decimal TotalAmount,
    Guid EmployerId,
    string EmployerName,
    string Reason,
    string Status,
    DateTime RaisedDate,
    string? ResolutionNote,
    DateTime? ResolvedDate
);


