using Members.Domain.Entities;
using Members.Domain.Repositories;
using PensionVault.Shared.Contracts;

namespace Members.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(INotificationRepository notificationRepo, IUnitOfWork unitOfWork)
    {
        _notificationRepo = notificationRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<NotificationResponse>> GetMyNotificationsAsync(Guid userId)
    {
        var notifications = await _notificationRepo.GetByUserAsync(userId);
        return notifications.Select(n => new NotificationResponse(
            n.NotificationId, n.UserId, n.Message, n.Category.ToString(), n.Status.ToString(), n.CreatedDate));
    }

    public async Task MarkReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _notificationRepo.FindByIdAndUserAsync(notificationId, userId)
            ?? throw new KeyNotFoundException("Notification not found.");
        notification.Status = NotificationStatus.Read;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        var notifications = await _notificationRepo.GetByUserAsync(userId);
        foreach (var n in notifications.Where(n => n.Status == NotificationStatus.Unread))
            n.Status = NotificationStatus.Read;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DismissAsync(Guid notificationId, Guid userId)
    {
        var notification = await _notificationRepo.FindByIdAndUserAsync(notificationId, userId)
            ?? throw new KeyNotFoundException("Notification not found.");
        notification.Status = NotificationStatus.Dismissed;
        await _unitOfWork.SaveChangesAsync();
    }
}




