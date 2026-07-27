namespace Claims.Domain.Entities;

public class BenefitClaim
{
    public Guid ClaimId { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public ClaimType ClaimType { get; set; }
    public DateTime ClaimDate { get; set; } = DateTime.UtcNow;
    public decimal EligibleAmount { get; set; }
    public decimal VestedAmount { get; set; }
    public decimal TaxDeductible { get; set; }
    public Guid? ProcessedById { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Submitted;

    // Reason for the withdrawal (Medical, Housing, Education, Marriage, Others, …)
    public string? Reason { get; set; }
    // Mandatory member-supplied explanation for the claim
    public string? Description { get; set; }
    // Timestamp of the last status change (review/approve/reject); disbursement date lives on the disbursement
    public DateTime? ProcessedDate { get; set; }
    // Reason captured when a claim is rejected
    public string? RejectionReason { get; set; }

    // Navigation
    public ICollection<ClaimDisbursement> Disbursements { get; set; } = new List<ClaimDisbursement>();
}

