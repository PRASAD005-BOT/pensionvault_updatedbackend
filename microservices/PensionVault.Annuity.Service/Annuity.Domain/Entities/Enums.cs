namespace Annuity.Domain.Entities;

public enum AnnuityPlanType { LifeAnnuity, JointAnnuity, TemporaryAnnuity, GuaranteedAnnuity }
public enum AnnuityStatus { Active, Suspended, Lapsed, Settled, Terminated }
public enum AnnuityRequestStatus { Pending, Approved, Rejected, Cancelled }
public enum PensionDisbursementStatus { Pending, Disbursed, Failed }

