using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace PensionVault.Shared.Http;

public abstract class BaseHttpRepository
{
    protected readonly HttpClient HttpClient;
    protected readonly IHttpContextAccessor HttpContextAccessor;
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static BaseHttpRepository()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    protected BaseHttpRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        HttpClient = httpClient;
        HttpContextAccessor = httpContextAccessor;
    }

    protected void ApplyAuthHeader()
    {
        var context = HttpContextAccessor.HttpContext;
        if (context != null && context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var val = authHeader.ToString();
            if (val.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                HttpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", val[7..]);
        }
    }

    protected async Task<T?> GetAsync<T>(string uri)
    {
        ApplyAuthHeader();
        var response = await HttpClient.GetAsync(uri);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return default;
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    protected async Task PostAsync<T>(string uri, T data)
    {
        ApplyAuthHeader();
        var response = await HttpClient.PostAsJsonAsync(uri, data, JsonOptions);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string uri, TRequest data)
    {
        ApplyAuthHeader();
        var response = await HttpClient.PostAsJsonAsync(uri, data, JsonOptions);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
    }

    protected async Task PutAsync<T>(string uri, T data)
    {
        ApplyAuthHeader();
        var response = await HttpClient.PutAsJsonAsync(uri, data, JsonOptions);
        response.EnsureSuccessStatusCode();
    }
}

