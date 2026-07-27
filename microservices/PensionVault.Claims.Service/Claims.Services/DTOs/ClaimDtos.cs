using Claims.Domain.Entities;

namespace Claims.Services.DTOs;

public record CreateClaimRequest(
    Guid MemberId,
    ClaimType ClaimType,
    decimal EligibleAmount,
    string? Reason = null,
    string? Description = null
);

public record ClaimActionRequest(string? Remarks);

public record DisburseClaimRequest(
    decimal DisbursedAmount,
    decimal TaxDeducted,
    string BankAccountRef
);

public record CreatePartialWithdrawalRequest(
    Guid MemberId,
    decimal RequestedAmount,
    string Reason,
    string? Description = null
);

public record DisbursePartialWithdrawalRequest(
    decimal DisbursedAmount,
    string BankAccountRef
);


