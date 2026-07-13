using Members.Domain.Repositories;
using PensionVault.Shared.Contracts;

namespace Members.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationResponse>> GetMyNotificationsAsync(Guid userId);
    Task MarkReadAsync(Guid notificationId, Guid userId);
    Task MarkAllReadAsync(Guid userId);
    Task DismissAsync(Guid notificationId, Guid userId);
}





