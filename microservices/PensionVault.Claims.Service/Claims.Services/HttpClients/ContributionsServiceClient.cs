using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using PensionVault.Shared.Contracts;

namespace Claims.Services.HttpClients;

public class ContributionsServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContributionsServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
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

    public async Task<FundAccountResponse?> GetActiveByMemberAsync(Guid memberId)
    {
        ApplyAuthHeader();
        var response = await _httpClient.GetAsync($"api/fundaccounts/active/member/{memberId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FundAccountResponse>();
    }

    public async Task AddLedgerEntryAsync(Guid accountId, string entryType, decimal amount, string referenceId)
    {
        ApplyAuthHeader();
        var entry = new
        {
            AccountId = accountId,
            EntryType = entryType,
            Amount = amount,
            ReferenceId = referenceId,
            Status = "Posted"
        };
        var response = await _httpClient.PostAsJsonAsync("api/ledger", entry);
        response.EnsureSuccessStatusCode();
    }
}




