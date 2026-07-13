namespace PensionVault.Shared.Contracts;

public record AuthResponse(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string Token,
    string RefreshToken,
    DateTime TokenExpiry,
    string? EmployeeId,
    string? ProfileImageUrl
);


