namespace PensionVault.Shared.Contracts;

public record CreateFundAccountRequest(
    Guid MemberId,
    Guid SchemeId,
    decimal VestingPercent,
    string? Status
);


