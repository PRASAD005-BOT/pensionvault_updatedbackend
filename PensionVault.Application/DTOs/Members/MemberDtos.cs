using System;
using System.ComponentModel.DataAnnotations;
using PensionVault.Domain.Enums;

namespace PensionVault.Application.DTOs.Members;

public record CreateMemberRequest(
    [Required] Guid UserId,

    [Required(AllowEmptyStrings = false, ErrorMessage = "Membership number is required and cannot be empty.")]
    string MembershipNumber,

    [Required(AllowEmptyStrings = false, ErrorMessage = "Name is required and cannot be empty.")]
    string Name,

    [Required]
    DateTime DateOfBirth,

    [Required(AllowEmptyStrings = false, ErrorMessage = "Gender is required.")]
    string? Gender,

    [Required(AllowEmptyStrings = false, ErrorMessage = "National identification is required.")]
    [RegularExpression(@"^(?:[A-Z]{5}[0-9]{4}[A-Z]{1}|[2-9]{1}[0-9]{11})$",
        ErrorMessage = "National ID must be a valid PAN format (e.g. ABCDE1234F) or a 12-digit national identifier.")]
    string? NationalIdRef,

    [Required] Guid EmployerId,

    [Required]
    DateTime JoiningDate,

    // Put it back here so MemberService.cs doesn't break!
    DateTime? DateOfRetirement,

    [Required(AllowEmptyStrings = false, ErrorMessage = "Nominee details are required.")]
    string? NomineeDetails
);

// Kept intact for compatibility with other workflows
public record UpdateMemberRequest(
    [Required(AllowEmptyStrings = false, ErrorMessage = "Name cannot be empty.")]
    string Name,

    [Required(AllowEmptyStrings = false, ErrorMessage = "Gender cannot be empty.")]
    string? Gender,

    [Required(AllowEmptyStrings = false, ErrorMessage = "National identification is required.")]
    [RegularExpression(@"^(?:[A-Z]{5}[0-9]{4}[A-Z]{1}|[2-9]{1}[0-9]{11})$",
        ErrorMessage = "National ID must be a valid PAN format or national identifier layout.")]
    string? NationalIdRef,

    DateTime? DateOfRetirement,
    string? NomineeDetails,
    MemberStatus Status
);

public record SelfEnrollMemberRequest(
    string NationalIdRef,
    DateTime DateOfBirth,
    string? Gender,
    Guid EmployerId,
    string? NomineeDetails
);

public record ApproveMemberRequest(
    string MembershipNumber,
    Guid EmployerId
);

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
    MemberStatus Status,
    string? ProfileImageUrl
);