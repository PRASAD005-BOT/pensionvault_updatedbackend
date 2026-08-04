using System.Net;
using System.Text;
using System.Text.Json;
using PensionVault.Shared.Contracts;

namespace Annuity_test;

/// <summary>
/// A minimal, hand-written <see cref="HttpMessageHandler"/> stub that lets the real
/// (concrete, non-virtual) service HTTP clients be exercised in unit tests without a
/// live server. A responder delegate decides the reply for each outgoing request.
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

/// <summary>Shared helpers and canned test data for the Annuity test suite.</summary>
public static class TestSupport
{
    public static HttpResponseMessage Json(object body, HttpStatusCode code = HttpStatusCode.OK)
        => new(code)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

    public static HttpResponseMessage NotFound() => new(HttpStatusCode.NotFound);
    public static HttpResponseMessage Ok() => new(HttpStatusCode.OK);

    public static MemberResponse Member(
        Guid? memberId = null,
        DateTime? dob = null,
        DateTime? joining = null,
        string status = "Active",
        Guid? userId = null)
        => new(
            memberId ?? Guid.NewGuid(),
            "PV0001",
            "Test Member",
            dob ?? DateTime.UtcNow.AddYears(-55),
            "Male",
            "AADHAAR-0000",
            Guid.NewGuid(),
            "Acme Technologies",
            joining ?? DateTime.UtcNow.AddYears(-15),
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
        decimal pensionBalance = 100000m,
        decimal totalBalance = 200000m)
        => new(
            Guid.NewGuid(),
            memberId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddYears(-15),
            100000m,
            50000m,
            pensionBalance,
            5000m,
            totalBalance,
            100m,
            "Active");
}
