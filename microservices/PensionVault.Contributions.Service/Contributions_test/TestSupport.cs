using System.Net;
using System.Text;
using System.Text.Json;
using PensionVault.Shared.Contracts;

namespace Contributions_test;

/// <summary>
/// Hand-written <see cref="HttpMessageHandler"/> stub used to drive the concrete
/// (non-virtual) service HTTP clients in unit tests without a live server.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => _responder = responder;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_responder(request));
}

/// <summary>Shared helpers and canned test data for the Contributions test suite.</summary>
public static class TestSupport
{
    public static HttpResponseMessage Json(object body, HttpStatusCode code = HttpStatusCode.OK)
        => new(code)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

    public static HttpResponseMessage NotFound() => new(HttpStatusCode.NotFound);
    public static HttpResponseMessage Ok() => new(HttpStatusCode.OK);

    public static SchemeResponse Scheme(Guid? schemeId = null, string name = "Employee Provident Fund")
        => new(
            schemeId ?? Guid.NewGuid(),
            name,
            "EPF",
            12m,
            12m,
            8.15m,
            5,
            100m,
            "Active",
            "Standard EPF scheme");
}
