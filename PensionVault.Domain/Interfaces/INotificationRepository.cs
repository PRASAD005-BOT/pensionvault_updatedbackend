using PensionVault.Domain.Entities;

namespace PensionVault.Domain.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetByUserAsync(Guid userId);
    Task<Notification?> FindByIdAndUserAsync(Guid notificationId, Guid userId);
    Task AddAsync(Notification notification);
    Task AddRangeAsync(IEnumerable<Notification> notifications);
}
