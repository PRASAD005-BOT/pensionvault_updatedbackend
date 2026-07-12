using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Claims.Service.ProxyRepositories;

public class HttpNotificationRepository : BaseHttpRepository, INotificationRepository
{
    public HttpNotificationRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public Task AddAsync(Notification notification)
        => PostAsync("api/notifications", notification);

    public Task AddRangeAsync(IEnumerable<Notification> notifications)
        => PostAsync("api/notifications/bulk", notifications.ToList());

    public Task<List<Notification>> GetByUserAsync(Guid userId) => throw new NotSupportedException();
    public Task<Notification?> FindByIdAndUserAsync(Guid notificationId, Guid userId) => throw new NotSupportedException();
}

