namespace Annuity.Domain.Entities;

public class AnnuityRequest
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public AnnuityPlanType PlanType { get; set; }
    public decimal PensionBalanceAtRequest { get; set; }
    public decimal EstimatedMonthly { get; set; }
    public string? Note { get; set; }
    public AnnuityRequestStatus Status { get; set; } = AnnuityRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewNote { get; set; }
}

