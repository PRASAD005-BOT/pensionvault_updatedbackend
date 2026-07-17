# PensionVault Module Mapping Guide

## Project Structure Overview

```
PensionVault-backend/
├── PensionVault.API/              # API Layer (Controllers, Middleware)
├── PensionVault.Application/      # Business Logic (Services, DTOs)
├── PensionVault.Domain/           # Data Models (Entities, Enums)
└── PensionVault.Infrastructure/   # Data Access (DbContext, Migrations)
```

---

## Module 2.1: Identity & Access Management (IAM)

### Purpose
Authentication, RBAC (Role-Based Access Control), and audit trails

### Code Location

#### Controllers
- **File**: `PensionVault.API/Controllers/AuthController.cs`
- **Endpoints**:
  - `POST /api/auth/login` - User authentication
  - `POST /api/auth/register` - User registration
  - `POST /api/auth/refresh` - Token refresh

#### Services
- **File**: `PensionVault.Application/Services/AuthService.cs`
- **Methods**:
  - `LoginAsync()` - Validate credentials, generate JWT tokens
  - `RegisterAsync()` - Create new user with role validation
  - `RefreshTokenAsync()` - Generate new access token using refresh token
  - `GenerateTokensAsync()` - Create JWT and refresh tokens

#### Domain Entities
- **User.cs**: User account with role, email, phone, password hash
  - Properties: UserId, Name, Email, Phone, Role, OrganisationId, Status, RefreshToken
  
- **AuditLog.cs**: Tracks all user actions
  - Properties: AuditId, UserId, Action, EntityType, RecordId, Timestamp

#### DTOs
- **Path**: `PensionVault.Application/DTOs/Auth/`
- **Files**:
  - `AuthDtos.cs`: LoginRequest, RegisterRequest, AuthResponse, RefreshTokenRequest

#### Database
- **Migrations**: `PensionVault.Infrastructure/Migrations/`
- **Tables**:
  - `Users` - User accounts with role and status
  - `AuditLogs` - Audit trail of all operations

#### Enums
- **UserRole**: `Admin`, `Member`, `Employer`, `FundAdmin`, `InvestmentOfficer`, `Compliance`
- **UserStatus**: `Active`, `Retired`, `Inactive`, `Deceased`

---

## Module 2.2: Member Enrolment & Account Management

### Purpose
Manages member registrations, employer linkages, and individual fund accounts

### Code Location

#### Controllers
- **File**: `PensionVault.API/Controllers/MembersController.cs`
- **Endpoints**:
  - `POST /api/members` - Create new member
  - `GET /api/members/{id}` - Get member details
  - `PUT /api/members/{id}` - Update member information
  - `GET /api/members` - List all members

#### Services
- **File**: `PensionVault.Application/Services/MemberService.cs`
- **Methods**:
  - `CreateMemberAsync()` - Register new member
  - `GetMemberAsync()` - Retrieve member by ID
  - `UpdateMemberAsync()` - Update member details
  - `GetAllMembersAsync()` - List members by organization

#### Domain Entities
- **Member.cs**: Individual pension scheme member
  - Properties: MemberId, UserId, MembershipNumber, Name, DateOfBirth, Gender, EmployerId, JoiningDate, Status
  
- **FundAccount.cs**: Individual member fund account
  - Properties: AccountId, MemberId, SchemeId, EmployeeContributionBalance, EmployerContributionBalance, Status

#### DTOs
- **Path**: `PensionVault.Application/DTOs/Members/`
- **Files**: Member registration, update, and response DTOs

#### Database
- **Tables**:
  - `Members` - Member profile with employer linkage
  - `FundAccounts` - Individual fund accounts per scheme

#### Enums
- **MemberStatus**: `Active`, `Resigned`, `Retired`, `Deceased`, `Transferred`
- **FundAccountStatus**: `Active`, `Settled`, `Frozen`

---

## Module 2.3: Contribution Processing & Employer Management

### Purpose
Handles monthly employer contribution remittances, reconciliation, and default tracking

### Code Location

#### Controllers
- **File**: `PensionVault.API/Controllers/EmployersController.cs`
- **File**: `PensionVault.API/Controllers/RemittancesController.cs`
- **Endpoints**:
  - `POST /api/employers` - Register employer
  - `GET /api/employers/{id}` - Get employer details
  - `POST /api/remittances` - Record contribution remittance
  - `GET /api/remittances` - List remittances

#### Services
- **File**: `PensionVault.Application/Services/EmployerService.cs`
- **File**: `PensionVault.Application/Services/ContributionService.cs`
- **Methods** (EmployerService):
  - `CreateEmployerAsync()` - Register new employer
  - `GetEmployerAsync()` - Retrieve employer details
  - `GetAllEmployersAsync()` - List employers
  
- **Methods** (ContributionService):
  - `ProcessRemittanceAsync()` - Record employer contributions
  - `ReconcileAsync()` - Reconcile remittances
  - `TrackDefaultAsync()` - Track non-compliant employers

#### Domain Entities
- **Employer.cs**: Employer organization
  - Properties: EmployerId, CompanyName, RegistrationNumber, Industry, RemittanceFrequency, Status
  
- **ContributionRemittance.cs**: Monthly contribution submission
  - Properties: RemittanceId, EmployerId, RemittancePeriod, TotalEmployeeShare, TotalEmployerShare, RemittanceDate, Status
  
- **MemberContribution.cs**: Individual member contribution record
  - Properties: ContributionId, MemberId, EmployeeAmount, EmployerAmount, PostedDate, Status

#### DTOs
- **Path**: `PensionVault.Application/DTOs/Employers/`
- **Path**: `PensionVault.Application/DTOs/Contributions/`

#### Database
- **Tables**:
  - `Employers` - Employer details with compliance status
  - `ContributionRemittances` - Monthly contributions
  - `MemberContributions` - Individual contribution postings

#### Enums
- **EmployerStatus**: `Active`, `Defaulter`, `Deregistered`
- **RemittanceFrequency**: `Monthly`, `Quarterly`
- **RemittanceStatus**: `Received`, `Reconciled`, `Shortfall`, `Default`
- **ContributionStatus**: `Posted`, `Reversed`, `Pending`

---

## Module 2.4: Member Account Ledger & Interest Crediting

### Purpose
Maintains individual member account ledgers with contribution postings and annual interest credits

### Code Location

#### Controllers
- **File**: `PensionVault.API/Controllers/LedgerController.cs`
- **Endpoints**:
  - `GET /api/ledger/{memberId}` - Get member ledger
  - `POST /api/ledger/interest-credit` - Record interest credit
  - `GET /api/ledger/summary` - Get account summary

#### Services
- **File**: `PensionVault.Application/Services/LedgerService.cs`
- **Methods**:
  - `GetMemberLedgerAsync()` - Retrieve member ledger entries
  - `CreditInterestAsync()` - Credit annual interest
  - `GetAccountSummaryAsync()` - Calculate account balance
  - `PostContributionAsync()` - Post contribution to ledger

#### Domain Entities
- **LedgerEntry.cs**: Individual ledger transaction
  - Properties: EntryId, AccountId, EntryType, Amount, BalanceAfter, EntryDate, Status
  
- **InterestCreditRecord.cs**: Annual interest accrual
  - Properties: InterestId, AccountId, InterestRate, InterestAmount, CreditDate, Status

#### DTOs
- **Path**: `PensionVault.Application/DTOs/Ledger/`

#### Database
- **Tables**:
  - `LedgerEntries` - All account transactions
  - `InterestCreditRecords` - Annual interest credits

#### Enums
- **EntryType**: `ContributionCredit`, `InterestCredit`, `PartialWithdrawal`, `TransferIn`, `TransferOut`, `ClaimDebit`
- **LedgerEntryStatus**: `Posted`, `Reversed`
- **InterestCreditStatus**: `Computed`, `Credited`

---

## Module 2.5: Benefit Claim & Withdrawal Management

### Purpose
Processes retirement, resignation, partial withdrawal, and nominee claim applications

### Code Location

#### Controllers
- **File**: `PensionVault.API/Controllers/ClaimsController.cs`
- **Endpoints**:
  - `POST /api/claims` - File claim application
  - `GET /api/claims/{id}` - Get claim status
  - `PUT /api/claims/{id}/approve` - Approve claim
  - `GET /api/claims` - List claims

#### Services
- **File**: `PensionVault.Application/Services/ClaimService.cs`
- **Methods**:
  - `SubmitClaimAsync()` - Create new claim
  - `GetClaimAsync()` - Retrieve claim details
  - `ApproveClaimAsync()` - Approve and process claim
  - `CalculateClaimAmountAsync()` - Compute claim settlement

#### Domain Entities
- **BenefitClaim.cs**: Claim application
  - Properties: ClaimId, MemberId, ClaimType, ClaimAmount, SubmittedDate, Status, ProcessedById
  
- **ClaimDisbursement.cs**: Claim payment record
  - Properties: DisbursementId, ClaimId, NetAmount, DisbursedDate, BankAccountRef, Status

#### DTOs
- **Path**: `PensionVault.Application/DTOs/Claims/`

#### Database
- **Tables**:
  - `BenefitClaims` - Claim applications
  - `ClaimDisbursements` - Claim payment records

#### Enums
- **ClaimType**: `Retirement`, `Resignation`, `PartialWithdrawal`, `DeathClaim`, `Disability`, `Marriage`, `Housing`
- **ClaimStatus**: `Submitted`, `UnderReview`, `Approved`, `Rejected`, `Disbursed`
- **DisbursementStatus**: `Pending`, `Processed`, `Failed`

---

## Module 2.6: Investment Fund Allocation & Corpus Management

### Purpose
Manages fund-level investment portfolio allocation, yield tracking, and corpus reporting

### Code Location

#### Controllers
- **File**: `PensionVault.API/Controllers/InvestmentController.cs`
- **File**: `PensionVault.API/Controllers/SchemesController.cs`
- **Endpoints**:
  - `GET /api/investments/{schemeId}` - Get fund portfolio
  - `POST /api/investments/allocate` - Allocate investments
  - `GET /api/corpus/{schemeId}` - Get corpus report

#### Services
- **File**: `PensionVault.Application/Services/InvestmentService.cs`
- **Methods**:
  - `GetPortfolioAsync()` - Retrieve investment allocation
  - `AllocateInvestmentsAsync()` - Update fund allocation
  - `GetCorpusReportAsync()` - Generate corpus report
  - `TrackYieldAsync()` - Calculate fund performance

#### Domain Entities
- **FundScheme.cs**: Pension scheme definition
  - Properties: SchemeId, SchemeName, SchemeType, EmployeeContributionRate, EmployerContributionRate, InterestRatePA
  
- **InvestmentPortfolio.cs**: Scheme investment allocation
  - Properties: PortfolioId, SchemeId, AssetClass, AllocationPercent, CurrentValue
  
- **CorpusRecord.cs**: Fund-level corpus tracking
  - Properties: CorpusId, SchemeId, RecordDate, TotalContributions, InvestmentIncome, ManagementExpenses

#### DTOs
- **Path**: `PensionVault.Application/DTOs/Investment/`

#### Database
- **Tables**:
  - `FundSchemes` - Scheme master records
  - `InvestmentPortfolios` - Asset allocation
  - `CorpusRecords` - Fund-level corpus

#### Enums
- **SchemeType**: `EPF`, `Gratuity`, `Superannuation`, `NPS`, `PPF`
- **SchemeStatus**: `Active`, `Closed`
- **AssetClass**: `GovernmentSecurities`, `CorporateBonds`, `Equity`, `FixedDeposit`, `MoneyMarket`
- **CorpusStatus**: `Draft`, `Finalised`

---

## Module 2.7: Pension Annuity & Settlement Management

### Purpose
Manages annuity plan selection, monthly pension disbursements, and nominee settlements

### Code Location

#### Controllers
- **File**: `PensionVault.API/Controllers/AnnuityController.cs`
- **Endpoints**:
  - `POST /api/annuity` - Create annuity plan
  - `GET /api/annuity/{memberId}` - Get annuity details
  - `POST /api/pension-disbursement` - Record pension payment

#### Services
- **File**: `PensionVault.Application/Services/AnnuityService.cs`
- **Methods**:
  - `CreateAnnuityPlanAsync()` - Set up annuity for retired member
  - `GetAnnuityAsync()` - Retrieve annuity details
  - `DisburseMonthlyPensionAsync()` - Process monthly pension payment
  - `SettleNomineeAsync()` - Process nominee settlement

#### Domain Entities
- **AnnuityPlan.cs**: Retirement annuity plan
  - Properties: AnnuityId, MemberId, PlanType, PurchaseValue, MonthlyPension, AnnuityStartDate, Status
  
- **MonthlyPensionDisbursement.cs**: Monthly pension payment record
  - Properties: DisbursementId, AnnuityId, Month, Year, GrossAmount, TaxDeducted, NetAmount, DisbursedDate, Status

#### DTOs
- **Path**: `PensionVault.Application/DTOs/Annuity/`

#### Database
- **Tables**:
  - `AnnuityPlans` - Annuity plan records
  - `MonthlyPensionDisbursements` - Monthly pension payments

#### Enums
- **AnnuityPlanType**: `LifeAnnuity`, `JointAnnuity`, `TemporaryAnnuity`, `GuaranteedAnnuity`
- **AnnuityStatus**: `Active`, `Suspended`, `Lapsed`, `Settled`
- **PensionDisbursementStatus**: `Pending`, `Disbursed`, `Failed`

---

## Module 2.8: Notifications & Alerts

### Purpose
Contribution credit confirmations, interest crediting alerts, claim status updates, compliance deadline reminders

### Code Location

#### Controllers
- **File**: `PensionVault.API/Controllers/NotificationsController.cs`
- **Endpoints**:
  - `GET /api/notifications` - Get user notifications
  - `PUT /api/notifications/{id}/read` - Mark notification as read
  - `POST /api/notifications/send` - Send notification (admin)

#### Services
- **File**: Built-in notification handling in other services
- **Trigger Points**:
  - ContributionService → Sends contribution credit notification
  - LedgerService → Sends interest credit alert
  - ClaimService → Sends claim status update
  - EmployerService → Sends compliance reminders

#### Domain Entities
- **Notification.cs**: User notification record
  - Properties: NotificationId, UserId, Message, Category, Status, CreatedDate

#### DTOs
- **Path**: `PensionVault.Application/DTOs/Misc/`

#### Database
- **Tables**:
  - `Notifications` - All user notifications

#### Enums
- **NotificationCategory**: `Contribution`, `Interest`, `Claim`, `Annuity`, `Compliance`, `Investment`
- **NotificationStatus**: `Unread`, `Read`, `Dismissed`

---

## Cross-Cutting Concerns

### Middleware
- **File**: `PensionVault.API/Middleware/ExceptionMiddleware.cs`
- **Purpose**: Global error handling and logging

### Database Context
- **File**: `PensionVault.Infrastructure/Data/AppDbContext.cs`
- **Purpose**: Entity Framework Core configuration and relationships

### Data Seeding
- **File**: `PensionVault.Infrastructure/Seeders/DataSeeder.cs`
- **Purpose**: Initial data setup for testing

### Authentication & Authorization
- JWT Bearer token-based authentication
- Role-based access control (RBAC) enforced at controller level
- Phone number validation (required for all users)
- Organization ID validation (required for Members and Employers)

---

## Module Dependency Graph

```
2.1 IAM (Auth)
├── Provides: User authentication, JWT tokens
└── Used by: All other modules

2.2 Member Management
├── Depends on: 2.1 IAM
├── Uses: 2.4 Ledger for account creation
└── Used by: 2.3, 2.5, 2.6, 2.7

2.3 Contribution Processing
├── Depends on: 2.1, 2.2
├── Updates: 2.4 Ledger
└── Triggers: 2.8 Notifications

2.4 Ledger & Interest
├── Depends on: 2.2, 2.3
└── Triggers: 2.8 Notifications

2.5 Claims & Withdrawals
├── Depends on: 2.1, 2.2, 2.4
├── Accesses: 2.6 Investment data
└── Triggers: 2.8 Notifications

2.6 Investment & Corpus
├── Depends on: 2.1
├── Accesses: 2.4 Ledger data
└── Triggers: 2.8 Notifications

2.7 Annuity & Pensions
├── Depends on: 2.2, 2.4
├── Accesses: 2.6 Investment data
└── Triggers: 2.8 Notifications

2.8 Notifications
└── Used by: All modules
```

---

## Quick Reference: Where to Find What

| Feature | Controller | Service | Entity | DTO Path |
|---------|-----------|---------|--------|----------|
| **Login/Register** | AuthController | AuthService | User, AuditLog | DTOs/Auth |
| **Member Registration** | MembersController | MemberService | Member, FundAccount | DTOs/Members |
| **Employer Setup** | EmployersController | EmployerService | Employer | DTOs/Employers |
| **Contributions** | RemittancesController | ContributionService | ContributionRemittance, MemberContribution | DTOs/Contributions |
| **Account Ledger** | LedgerController | LedgerService | LedgerEntry, InterestCreditRecord | DTOs/Ledger |
| **Claims** | ClaimsController | ClaimService | BenefitClaim, ClaimDisbursement | DTOs/Claims |
| **Investment** | InvestmentController | InvestmentService | InvestmentPortfolio, CorpusRecord | DTOs/Investment |
| **Schemes** | SchemesController | (Embedded in services) | FundScheme | DTOs/Misc |
| **Annuity** | AnnuityController | AnnuityService | AnnuityPlan, MonthlyPensionDisbursement | DTOs/Annuity |
| **Notifications** | NotificationsController | (Triggered by services) | Notification | DTOs/Misc |

