namespace Members.Domain.Entities;

public class Member
{
    public Guid MemberId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? NationalIdRef { get; set; }
    public Guid EmployerId { get; set; }
    public DateTime JoiningDate { get; set; }
    public DateTime? DateOfRetirement { get; set; }
    public string? NomineeDetails { get; set; } // JSON
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    // Navigation
    public User? User { get; set; }
    public Employer? Employer { get; set; }
}

