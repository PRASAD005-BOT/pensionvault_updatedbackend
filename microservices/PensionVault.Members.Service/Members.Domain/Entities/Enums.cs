namespace Members.Domain.Entities;

public enum UserRole { Member, Employer, FundAdmin, InvestmentOfficer, Compliance, Admin }
public enum UserStatus { Active, Retired, Inactive, Deceased }
public enum SchemeType { EPF, Gratuity, Superannuation, NPS, PPF }
public enum SchemeStatus { Active, Closed }
public enum EmployerStatus { Active, Defaulter, Deregistered }
public enum RemittanceFrequency { Monthly, Quarterly }
public enum MemberStatus { Active, Resigned, Retired, Deceased, Transferred }
public enum NotificationCategory { Contribution, Interest, Claim, Annuity, Compliance, Investment }
public enum NotificationStatus { Unread, Read, Dismissed }

