using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PensionVault.Shared.Contracts;

namespace Annuity.Services.HttpClients;

public class NotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<NotificationServiceClient> _logger;

    public NotificationServiceClient(
        HttpClient httpClient, 
        IHttpContextAccessor httpContextAccessor,
        ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private void ApplyAuthHeader()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null && context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var val = authHeader.ToString();
            if (val.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", val[7..]);
        }
    }

    public async Task SendBulkNotificationsAsync(List<CreateNotificationRequest> requests)
    {
        try
        {
            ApplyAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/notifications/bulk", requests);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send bulk notifications. Status code: {StatusCode}, Content: {Content}", response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception when calling Notification Service: {Message}", ex.Message);
        }
    }
}




