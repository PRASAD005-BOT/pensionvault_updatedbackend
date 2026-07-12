using PensionVault.Domain.Enums;

namespace PensionVault.Domain.Entities;

/// <summary>
/// Represents a member's request to convert their pension corpus into an annuity plan.
/// Persisted in the database — replaces any client-side/localStorage approach.
/// </summary>
public class AnnuityRequest
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public AnnuityPlanType PlanType { get; set; }

    /// <summary>Member's pension balance at the time of the request (purchase value).</summary>
    public decimal PensionBalanceAtRequest { get; set; }

    /// <summary>Estimated monthly pension calculated at request time.</summary>
    public decimal EstimatedMonthly { get; set; }

    public string? Note { get; set; }
    public AnnuityRequestStatus Status { get; set; } = AnnuityRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Admin/FundAdmin userId who approved or rejected the request.</summary>
    public Guid? ReviewedByUserId { get; set; }

    public string? ReviewNote { get; set; }

}
