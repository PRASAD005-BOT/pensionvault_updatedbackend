using System.ComponentModel.DataAnnotations;
using Members.Domain.Entities;

namespace Members.Services.DTOs;

public record CreateMemberRequest(
    Guid UserId,
    string MembershipNumber,
    string Name,
    DateTime DateOfBirth,
    string? Gender,
    [MaxLength(12)] string? NationalIdRef,
    Guid EmployerId,
    DateTime JoiningDate,
    DateTime? DateOfRetirement,
    string? NomineeDetails,
    string Email
);

public record UpdateMemberRequest(
    string Name,
    DateTime DateOfBirth,
    string? Gender,
    [MaxLength(12)] string? NationalIdRef,
    DateTime? DateOfRetirement,
    string? NomineeDetails,
    MemberStatus Status,
    Guid EmployerId,
    DateTime JoiningDate,
    string Email
);

public record SelfEnrollMemberRequest(
    [MaxLength(12)] string NationalIdRef,
    DateTime DateOfBirth,
    string? Gender,
    Guid EmployerId,
    string? NomineeDetails
);

public record ApproveMemberRequest(
    string MembershipNumber,
    Guid EmployerId
);


