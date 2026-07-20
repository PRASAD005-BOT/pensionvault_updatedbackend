namespace Contributions.Domain.Entities;

public enum FundAccountStatus { Active, Settled, Frozen }
public enum RemittanceFrequency { Monthly, Quarterly }
public enum EmployerStatus { Active, Defaulter, Deregistered }
public enum RemittanceStatus { Received, Reconciled, Shortfall, Default }
public enum ContributionStatus { Posted, Reversed, Pending }
public enum EntryType { ContributionCredit, InterestCredit, PartialWithdrawal, TransferIn, TransferOut, ClaimDebit, AnnuityDebit, PensionCredit }
public enum LedgerEntryStatus { Posted, Reversed }
public enum InterestCreditStatus { Computed, Credited }
public enum AssetClass { GovernmentSecurities, CorporateBonds, Equity, FixedDeposit, MoneyMarket, MutualFunds }
public enum CorpusStatus { Draft, Finalised }
public enum SchemeType { EPF, Gratuity, Superannuation, NPS, PPF }
public enum SchemeStatus { Active, Closed }
public enum ShortfallRequestStatus { Raised, Resolved, Rejected }

