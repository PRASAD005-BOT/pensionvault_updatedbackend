using Claims.Domain.Entities;

namespace Claims.Services.DTOs;

public record CreateClaimRequest(
    Guid MemberId,
    ClaimType ClaimType,
    decimal EligibleAmount
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
    string Reason
);

public record DisbursePartialWithdrawalRequest(
    decimal DisbursedAmount,
    string BankAccountRef
);


