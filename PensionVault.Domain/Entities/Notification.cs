using PensionVault.Domain.Enums;

namespace PensionVault.Domain.Entities;

public class Notification
{
    public Guid NotificationId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationCategory Category { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
