using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionVault.Application.DTOs.Misc;
using PensionVault.Application.Services;
using System.Security.Claims;

namespace PensionVault.API.Controllers;

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
        return Ok(await _notificationService.GetUserNotificationsAsync(userId));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        return Ok(result);
    }

    [HttpPost("send-email")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> SendEmail([FromBody] EmailNotificationRequest request)
    {
        var success = await _notificationService.SendEmailNotificationAsync(request);
        return Ok(new { success });
    }

    [HttpPost("send-sms")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> SendSms([FromBody] SmsNotificationRequest request)
    {
        var success = await _notificationService.SendSmsNotificationAsync(request);
        return Ok(new { success });
    }
}
