using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;
    public NotificationRepository(AppDbContext context) => _context = context;

    public Task<List<Notification>> GetByUserAsync(Guid userId)
        => _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();

    public Task<Notification?> FindByIdAndUserAsync(Guid notificationId, Guid userId)
        => _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

    public async Task AddAsync(Notification notification)
        => await _context.Notifications.AddAsync(notification);

    public async Task AddRangeAsync(IEnumerable<Notification> notifications)
        => await _context.Notifications.AddRangeAsync(notifications);
}
