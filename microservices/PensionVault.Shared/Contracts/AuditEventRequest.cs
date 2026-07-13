namespace PensionVault.Shared.Contracts;

public record AuditEventRequest(
    Guid UserId,
    string Action,
    string EntityType,
    string? RecordId
);


