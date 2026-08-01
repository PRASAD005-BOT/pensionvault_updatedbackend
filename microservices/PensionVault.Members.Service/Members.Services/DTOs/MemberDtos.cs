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
    string? NomineeName,
    string? NomineeRelation,
    string? NomineeBankAccount,
    int? NomineePercent,
    string Email
);

public record UpdateMemberRequest(
    string Name,
    DateTime DateOfBirth,
    string? Gender,
    [MaxLength(12)] string? NationalIdRef,
    DateTime? DateOfRetirement,
    string? NomineeName,
    string? NomineeRelation,
    string? NomineeBankAccount,
    int? NomineePercent,
    MemberStatus Status,
    Guid EmployerId,
    DateTime JoiningDate,
    string Email,
    string? Phone
);

public record SelfEnrollMemberRequest(
    [MaxLength(12)] string NationalIdRef,
    DateTime DateOfBirth,
    string? Gender,
    Guid EmployerId,
    string? NomineeName,
    string? NomineeRelation,
    string? NomineeBankAccount,
    int? NomineePercent,
    string Phone,
    DateTime JoiningDate
);

public record ApproveMemberRequest(
    string MembershipNumber,
    Guid EmployerId
);

/// <summary>Lightweight DTO for status-only updates (e.g. Resigned/Retired after claim disbursement).</summary>
public record UpdateMemberStatusRequest(MemberStatus Status);
