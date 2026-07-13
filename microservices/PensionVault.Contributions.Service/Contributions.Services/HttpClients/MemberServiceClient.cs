using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using PensionVault.Shared.Contracts;

namespace Contributions.Services.HttpClients;

public class MemberServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MemberServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
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

    public async Task<EmployerResponse?> GetEmployerByIdAsync(Guid employerId)
    {
        ApplyAuthHeader();
        var response = await _httpClient.GetAsync($"api/employers/{employerId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmployerResponse>();
    }

    public async Task<SchemeResponse?> GetSchemeByIdAsync(Guid schemeId)
    {
        ApplyAuthHeader();
        var response = await _httpClient.GetAsync($"api/schemes/{schemeId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SchemeResponse>();
    }

    public async Task<List<EmployerResponse>> GetAllEmployersAsync()
    {
        ApplyAuthHeader();
        var response = await _httpClient.GetAsync("api/employers");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return new List<EmployerResponse>();
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<EmployerResponse>>() ?? new List<EmployerResponse>();
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
}




