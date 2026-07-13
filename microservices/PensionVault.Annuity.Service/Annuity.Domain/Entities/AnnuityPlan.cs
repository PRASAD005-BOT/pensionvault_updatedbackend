namespace Annuity.Domain.Entities;

public class AnnuityPlan
{
    public Guid AnnuityId { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public AnnuityPlanType PlanType { get; set; }
    public decimal PurchaseValue { get; set; }
    public decimal MonthlyPension { get; set; }
    public DateTime AnnuityStartDate { get; set; }
    public string? NomineeDetails { get; set; } // JSON
    public AnnuityStatus Status { get; set; } = AnnuityStatus.Active;

    // Navigation
    public ICollection<MonthlyPensionDisbursement> PensionDisbursements { get; set; } = new List<MonthlyPensionDisbursement>();
}

