namespace Contributions.Domain.Entities;

public class ShortfallRequest
{
    public Guid ShortfallRequestId { get; set; } = Guid.NewGuid();
    public Guid ContributionId { get; set; }
    public Guid MemberId { get; set; }
    public Guid EmployerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ShortfallRequestStatus Status { get; set; } = ShortfallRequestStatus.Raised;
    public DateTime RaisedDate { get; set; } = DateTime.UtcNow;
    public string? ResolutionNote { get; set; }
    public DateTime? ResolvedDate { get; set; }

    // Navigation
    public MemberContribution? Contribution { get; set; }
}
