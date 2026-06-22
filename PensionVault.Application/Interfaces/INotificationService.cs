using PensionVault.Application.DTOs.Notifications;
using PensionVault.Domain.Enums;

namespace PensionVault.Application.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationResponse>> GetMyNotificationsAsync(Guid userId);
    Task MarkReadAsync(Guid notificationId, Guid userId);
    Task MarkAllReadAsync(Guid userId);
    Task DismissAsync(Guid notificationId, Guid userId);
}
