using PensionVault.Application.DTOs.Misc;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PensionVault.Application.Services;

public interface INotificationService
{
    Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request);
    Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId);
    Task<NotificationResponse> MarkAsReadAsync(Guid notificationId);
    Task<bool> SendEmailNotificationAsync(EmailNotificationRequest request);
    Task<bool> SendSmsNotificationAsync(SmsNotificationRequest request);
    Task CreateAndNotifyAsync(Guid userId, NotificationCategory category, string message, string? emailTo = null, string? phoneTo = null);
}

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _context;
    
    public NotificationService(IAppDbContext context) => _context = context;

    public async Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Category = request.Category,
            Title = request.Title,
            Message = request.Message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        
        return ToResponse(notification);
    }

    public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => ToResponse(n))
            .ToListAsync();
    }

    public async Task<NotificationResponse> MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId)
            ?? throw new KeyNotFoundException("Notification not found.");
        
        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return ToResponse(notification);
    }

    public async Task<bool> SendEmailNotificationAsync(EmailNotificationRequest request)
    {
        try
        {
            // TODO: Integrate with email service (SendGrid, SMTP, etc.)
            // For now, this is a placeholder implementation
            
            // Example: await _emailService.SendAsync(request.ToEmail, request.Subject, request.Body);
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.ToEmail);
            if (user != null)
            {
                await CreateNotificationAsync(new CreateNotificationRequest(
                    user.UserId,
                    NotificationCategory.Alert,
                    request.Subject,
                    request.Body
                ));
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendSmsNotificationAsync(SmsNotificationRequest request)
    {
        try
        {
            // TODO: Integrate with SMS service (Twilio, AWS SNS, etc.)
            // For now, this is a placeholder implementation
            
            // Example: await _smsService.SendAsync(request.PhoneNumber, request.Message);
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == request.PhoneNumber);
            if (user != null)
            {
                await CreateNotificationAsync(new CreateNotificationRequest(
                    user.UserId,
                    NotificationCategory.Alert,
                    "SMS Alert",
                    request.Message
                ));
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task CreateAndNotifyAsync(Guid userId, NotificationCategory category, string message, string? emailTo = null, string? phoneTo = null)
    {
        // Create in-app notification
        await CreateNotificationAsync(new CreateNotificationRequest(
            userId,
            category,
            category.ToString(),
            message
        ));

        // Send email if provided
        if (!string.IsNullOrEmpty(emailTo))
        {
            await SendEmailNotificationAsync(new EmailNotificationRequest(
                emailTo,
                category.ToString(),
                message
            ));
        }

        // Send SMS if provided
        if (!string.IsNullOrEmpty(phoneTo))
        {
            await SendSmsNotificationAsync(new SmsNotificationRequest(
                phoneTo,
                message
            ));
        }
    }

    private static NotificationResponse ToResponse(Notification n) => new(
        n.NotificationId,
        n.UserId,
        n.Category,
        n.Title,
        n.Message,
        n.IsRead,
        n.CreatedAt,
        n.ReadAt
    );
}
