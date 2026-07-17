using System.ComponentModel.DataAnnotations;

namespace Members.Services.DTOs;

public record LoginRequest([EmailAddress] string Email, string Password, string? Role = null);

public record RegisterRequest(
    string Name,
    [EmailAddress] string Email,
    [MinLength(6)] string Password,
    string Role,
    string? Phone,
    Guid? OrganisationId,
    string? EmployeeId,
    string? CompanyName = null
);

public record RefreshTokenRequest(string RefreshToken);


