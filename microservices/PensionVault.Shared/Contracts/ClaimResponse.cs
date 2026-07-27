namespace PensionVault.Shared.Contracts;

public record ClaimResponse(
    Guid ClaimId,
    Guid MemberId,
    string MemberName,
    string ClaimType,
    DateTime ClaimDate,
    decimal EligibleAmount,
    decimal VestedAmount,
    decimal TaxDeductible,
    string? ProcessedByName,
    string Status,
    string? Reason = null,
    string? Description = null,
    DateTime? ProcessedDate = null,
    string? RejectionReason = null,
    DateTime? DisbursedDate = null,
    string? TransactionReference = null
);

public record DisbursementResponse(
    Guid DisbursementId,
    Guid ClaimId,
    decimal DisbursedAmount,
    decimal TaxDeducted,
    decimal NetAmount,
    string? BankAccountRef,
    DateTime? DisbursedDate,
    string Status
);


public record PartialWithdrawalRequest(
    Guid MemberId,
    decimal WithdrawalAmount,
    string Remarks
);

public record PartialWithdrawalDisbursementRequest(
    decimal DisbursedAmount,
    decimal TaxDeducted,
    string BankAccountRef
);
