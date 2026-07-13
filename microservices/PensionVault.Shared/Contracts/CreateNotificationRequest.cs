namespace PensionVault.Shared.Contracts;

public record CreateNotificationRequest(
    Guid UserId,
    string Message,
    string Category
);


