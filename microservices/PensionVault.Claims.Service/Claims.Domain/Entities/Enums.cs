namespace Claims.Domain.Entities;

public enum ClaimType { Retirement, Resignation, PartialWithdrawal, DeathClaim, Disability, Marriage, Housing }
public enum ClaimStatus { Submitted, UnderReview, Approved, Rejected, Disbursed }
public enum DisbursementStatus { Pending, Processed, Failed }

