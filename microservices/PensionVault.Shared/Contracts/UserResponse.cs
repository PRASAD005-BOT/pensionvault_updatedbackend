namespace PensionVault.Shared.Contracts;

public record UserResponse(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string Status,
    Guid? OrganisationId,
    string? EmployeeId
);

public record UserSummaryResponse(
    Guid UserId,
    string Name,
    string Email,
    string Role
);


