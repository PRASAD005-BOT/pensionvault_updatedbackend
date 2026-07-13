namespace PensionVault.Shared.Contracts;

public record NotificationResponse(
    Guid NotificationId,
    Guid UserId,
    string Message,
    string Category,
    string Status,
    DateTime CreatedDate
);


