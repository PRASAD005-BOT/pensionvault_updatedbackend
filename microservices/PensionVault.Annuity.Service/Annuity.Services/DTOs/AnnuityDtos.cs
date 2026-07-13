using Annuity.Domain.Entities;

namespace Annuity.Services.DTOs;

public record CreateAnnuityRequest(
    Guid MemberId,
    AnnuityPlanType PlanType,
    decimal PurchaseValue,
    decimal MonthlyPension,
    DateTime AnnuityStartDate,
    string? NomineeDetails
);

public record UpdateAnnuityRequest(
    AnnuityPlanType PlanType,
    decimal PurchaseValue,
    decimal MonthlyPension,
    string? NomineeDetails,
    AnnuityStatus? Status
);

public record AnnuityResponse(
    Guid AnnuityId,
    Guid MemberId,
    string MemberName,
    AnnuityPlanType PlanType,
    decimal PurchaseValue,
    decimal MonthlyPension,
    DateTime AnnuityStartDate,
    string? NomineeDetails,
    AnnuityStatus Status,
    bool IsDisbursedThisMonth = false
);

public record ProcessDisbursementRequest(
    Guid AnnuityId,
    int Month,
    int Year,
    decimal TaxDeducted
);

public record PensionDisbursementResponse(
    Guid DisbursementId,
    Guid AnnuityId,
    Guid MemberId,
    string MemberName,
    int Month,
    int Year,
    decimal GrossAmount,
    decimal TaxDeducted,
    decimal NetAmount,
    DateTime? DisbursedDate,
    PensionDisbursementStatus Status
);

public record NomineeSettlementRequest(
    string NomineeName,
    string BankAccountRef,
    decimal SettlementAmount
);

public record SubmitAnnuityRequestDto(
    Guid MemberId,
    AnnuityPlanType PlanType,
    string? Note
);

public record AnnuityRequestResponse(
    Guid RequestId,
    Guid MemberId,
    string MemberName,
    string MembershipNumber,
    AnnuityPlanType PlanType,
    decimal PensionBalanceAtRequest,
    decimal EstimatedMonthly,
    string? Note,
    AnnuityRequestStatus Status,
    DateTime RequestedAt,
    DateTime? ReviewedAt,
    string? ReviewNote
);

public record AnnuityEligibilityResponse(
    Guid MemberId,
    bool IsEligible,
    int AgeYears,
    int ServiceYears,
    int ContributionMonths,
    decimal PensionBalance,
    List<string> FailureReasons
);


