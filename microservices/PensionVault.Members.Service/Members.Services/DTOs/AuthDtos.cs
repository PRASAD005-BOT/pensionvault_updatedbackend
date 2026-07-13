namespace Members.Services.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string Role,
    string? Phone,
    Guid? OrganisationId,
    string? EmployeeId
);

public record RefreshTokenRequest(string RefreshToken);


