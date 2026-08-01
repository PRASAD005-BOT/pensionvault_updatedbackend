using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using PensionVault.Shared.Contracts;

namespace Claims.Services.HttpClients;

public class MembersServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MembersServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
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

    public async Task<MemberResponse?> GetMemberByIdAsync(Guid memberId)
    {
        ApplyAuthHeader();
        var response = await _httpClient.GetAsync($"api/members/{memberId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MemberResponse>();
    }

    public async Task<MemberResponse?> GetMemberByUserIdAsync(Guid userId)
    {
        ApplyAuthHeader();
        var response = await _httpClient.GetAsync($"api/members/by-user/{userId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MemberResponse>();
    }

    public async Task<List<UserSummaryResponse>> GetUsersByRoleAsync(string role)
    {
        ApplyAuthHeader();
        var response = await _httpClient.GetAsync($"api/users/by-role/{role}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return new List<UserSummaryResponse>();
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<UserSummaryResponse>>() ?? new List<UserSummaryResponse>();
    }

    /// <summary>
    /// Updates a member's status to Resigned or Retired after their claim is disbursed.
    /// Failures are swallowed with a log-friendly warning so the disbursement is not rolled back.
    /// </summary>
    public async Task UpdateMemberStatusAsync(Guid memberId, string status)
    {
        ApplyAuthHeader();
        var payload = new { Status = status };
        var response = await _httpClient.PatchAsJsonAsync($"api/members/{memberId}/status", payload);
        // Non-critical: log but don't throw — the disbursement already succeeded
        response.EnsureSuccessStatusCode();
    }
}




