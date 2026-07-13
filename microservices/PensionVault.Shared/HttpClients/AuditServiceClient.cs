using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PensionVault.Shared.Contracts;

namespace PensionVault.Shared.HttpClients;

public class AuditServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuditServiceClient> _logger;

    public AuditServiceClient(HttpClient httpClient, ILogger<AuditServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task WriteAsync(Guid userId, string action, string entityType, string? recordId)
    {
        try
        {
            var request = new AuditEventRequest(userId, action, entityType, recordId);
            var response = await _httpClient.PostAsJsonAsync("api/audit", request);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to write audit log. Status code: {StatusCode}, Content: {Content}", response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            // Do not break execution of the business flow if audit logging fails
            _logger.LogError(ex, "Exception when calling Audit Service client: {Message}", ex.Message);
        }
    }
}


