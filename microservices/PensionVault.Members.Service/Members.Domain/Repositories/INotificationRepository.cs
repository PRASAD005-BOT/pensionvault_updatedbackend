using Members.Domain.Entities;

namespace Members.Domain.Repositories;

public interface INotificationRepository
{
    Task<List<Notification>> GetByUserAsync(Guid userId);
    Task<Notification?> FindByIdAndUserAsync(Guid notificationId, Guid userId);
    Task AddAsync(Notification notification);
    Task AddRangeAsync(IEnumerable<Notification> notifications);
}


