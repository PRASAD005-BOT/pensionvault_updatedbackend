using PensionVault.Domain.Enums;

namespace PensionVault.Application.DTOs.Misc;

public record CreateNotificationRequest(
    Guid UserId,
    NotificationCategory Category,
    string Title,
    string Message
);

public record NotificationResponse(
    Guid NotificationId,
    Guid UserId,
    NotificationCategory Category,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt
);

public record EmailNotificationRequest(
    string ToEmail,
    string Subject,
    string Body
);

public record SmsNotificationRequest(
    string PhoneNumber,
    string Message
);
