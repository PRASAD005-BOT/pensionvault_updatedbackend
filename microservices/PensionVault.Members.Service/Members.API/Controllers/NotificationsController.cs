using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Members.Services;
using Members.Domain.Repositories;
using Members.Domain.Entities;
using PensionVault.Shared.Contracts;
using System.Security.Claims;

namespace Members.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    public NotificationsController(INotificationService notificationService)
        => _notificationService = notificationService;

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notifications = await _notificationService.GetMyNotificationsAsync(userId);
        return Ok(notifications);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _notificationService.MarkReadAsync(id, userId);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _notificationService.MarkAllReadAsync(userId);
        return NoContent();
    }

    [HttpPut("{id:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _notificationService.DismissAsync(id, userId);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> CreateNotification(
        [FromServices] INotificationRepository notificationRepo,
        [FromServices] IUnitOfWork unitOfWork,
        [FromBody] CreateNotificationRequest request)
    {
        var category = Enum.TryParse<NotificationCategory>(request.Category, true, out var c) ? c : NotificationCategory.Compliance;
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = request.UserId,
            Message = request.Message,
            Category = category,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        };

        await notificationRepo.AddAsync(notification);
        await unitOfWork.SaveChangesAsync();
        return Ok(notification);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> CreateNotifications(
        [FromServices] INotificationRepository notificationRepo,
        [FromServices] IUnitOfWork unitOfWork,
        [FromBody] List<CreateNotificationRequest> requests)
    {
        var notifications = requests.Select(request => {
            var category = Enum.TryParse<NotificationCategory>(request.Category, true, out var c) ? c : NotificationCategory.Compliance;
            return new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = request.UserId,
                Message = request.Message,
                Category = category,
                Status = NotificationStatus.Unread,
                CreatedDate = DateTime.UtcNow
            };
        }).ToList();

        await notificationRepo.AddRangeAsync(notifications);
        await unitOfWork.SaveChangesAsync();
        return Ok(notifications);
    }
}



