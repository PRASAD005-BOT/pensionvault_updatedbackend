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
    string Status
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

<<<<<<< HEAD:microservices/PensionVault.Shared/Contracts/ClaimResponse.cs

=======
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
>>>>>>> 62da4b668caf28d68984d044e8849bcee250dad4:PensionVault.Application/DTOs/Claims/ClaimDtos.cs
