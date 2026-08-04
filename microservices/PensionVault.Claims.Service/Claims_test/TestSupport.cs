using System.Net;
using System.Text;
using System.Text.Json;
using PensionVault.Shared.Contracts;

namespace Claims_test;

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

/// <summary>Shared helpers and canned test data for the Claims test suite.</summary>
public static class TestSupport
{
    public static HttpResponseMessage Json(object body, HttpStatusCode code = HttpStatusCode.OK)
        => new(code)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

    public static HttpResponseMessage NotFound() => new(HttpStatusCode.NotFound);
    public static HttpResponseMessage Ok() => new(HttpStatusCode.OK);

    public static MemberResponse Member(Guid? memberId = null, Guid? userId = null, string status = "Active")
        => new(
            memberId ?? Guid.NewGuid(),
            "PV0001",
            "Test Member",
            DateTime.UtcNow.AddYears(-40),
            "Male",
            "AADHAAR-0000",
            Guid.NewGuid(),
            "Acme Technologies",
            DateTime.UtcNow.AddYears(-10),
            null,
            "Spouse Nominee",
            "Spouse",
            "ACC-123456",
            100,
            status,
            null,
            "member@test.com",
            userId ?? Guid.NewGuid(),
            "9999999999");

    public static FundAccountResponse Account(
        Guid? memberId = null,
        decimal totalBalance = 200000m,
        decimal vestingPercent = 100m,
        decimal pensionBalance = 50000m)
        => new(
            Guid.NewGuid(),
            memberId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddYears(-10),
            120000m,
            80000m,
            pensionBalance,
            5000m,
            totalBalance,
            vestingPercent,
            "Active");
}
