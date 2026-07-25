namespace PensionVault.Shared.Results;

/// <summary>
/// Represents the outcome of a service operation without using exceptions for
/// <em>expected</em> failures (invalid credentials, conflicts, validation).
/// Controllers map <see cref="StatusCode"/> + <see cref="Error"/> to an HTTP
/// response. Reserve thrown exceptions for genuinely unexpected conditions.
/// </summary>
public record ServiceResult<T>
{
    public bool Success { get; }
    public T? Value { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    private ServiceResult(bool success, T? value, string? error, int statusCode)
    {
        Success = success;
        Value = value;
        Error = error;
        StatusCode = statusCode;
    }

    public static ServiceResult<T> Ok(T value) => new(true, value, null, 200);

    public static ServiceResult<T> Fail(string error, int statusCode) =>
        new(false, default, error, statusCode);
}