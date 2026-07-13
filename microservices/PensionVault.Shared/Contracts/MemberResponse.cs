namespace PensionVault.Shared.Contracts;

public record MemberResponse(
    Guid MemberId,
    string MembershipNumber,
    string Name,
    DateTime DateOfBirth,
    string? Gender,
    string? NationalIdRef,
    Guid EmployerId,
    string EmployerName,
    DateTime JoiningDate,
    DateTime? DateOfRetirement,
    string? NomineeDetails,
    string Status, // Keep as string for simple transmission
    string? ProfileImageUrl,
    string Email,
    Guid UserId
);


