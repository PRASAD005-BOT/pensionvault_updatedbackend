using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using System.Security.Claims;

namespace PensionVault.Members.Service.Controllers;

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
        return Ok(notifications.Select(n => new
        {
            n.NotificationId, n.Message,
            Category = n.Category.ToString(),
            Status = n.Status.ToString(),
            n.CreatedDate
        }));
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
        [FromServices] PensionVault.Domain.Interfaces.INotificationRepository notificationRepo,
        [FromServices] PensionVault.Domain.Interfaces.IUnitOfWork unitOfWork,
        [FromBody] Notification notification)
    {
        await notificationRepo.AddAsync(notification);
        await unitOfWork.SaveChangesAsync();
        return Ok(notification);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> CreateNotifications(
        [FromServices] PensionVault.Domain.Interfaces.INotificationRepository notificationRepo,
        [FromServices] PensionVault.Domain.Interfaces.IUnitOfWork unitOfWork,
        [FromBody] List<Notification> notifications)
    {
        await notificationRepo.AddRangeAsync(notifications);
        await unitOfWork.SaveChangesAsync();
        return Ok(notifications);
    }
}

