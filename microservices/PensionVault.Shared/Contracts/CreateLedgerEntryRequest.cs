namespace PensionVault.Shared.Contracts;

public record CreateLedgerEntryRequest(
    Guid AccountId,
    string EntryType,
    decimal Amount,
    string? ReferenceId,
    string? Status
);


