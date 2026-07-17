# PensionVault Architecture Overview

## Layered Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     API Layer (Presentation)                    │
│                    PensionVault.API/Controllers                 │
├─────────────────────────────────────────────────────────────────┤
│  Auth  │ Members │ Employers │ Remittances │ Ledger │ Claims   │
│ Annuity│Investment│Schemes│Notifications│Reports                │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                 Business Logic Layer (Services)                 │
│              PensionVault.Application/Services                  │
├─────────────────────────────────────────────────────────────────┤
│ AuthService │ MemberService │ EmployerService │ ClaimService   │
│ ContributionService │ LedgerService │ InvestmentService        │
│ AnnuityService │ [Notification Triggers]                        │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│             Data Transfer Objects & Validation                  │
│               PensionVault.Application/DTOs                     │
├─────────────────────────────────────────────────────────────────┤
│ Auth│Members│Employers│Contributions│Ledger│Claims│Investment  │
│ Annuity│Misc                                                    │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                   Domain Model Layer (Entities)                 │
│                 PensionVault.Domain/Entities                    │
├─────────────────────────────────────────────────────────────────┤
│ User │ Member │ Employer │ FundScheme │ FundAccount            │
│ ContributionRemittance │ MemberContribution │ LedgerEntry      │
│ InterestCreditRecord │ BenefitClaim │ ClaimDisbursement       │
│ InvestmentPortfolio │ CorpusRecord │ AnnuityPlan              │
│ MonthlyPensionDisbursement │ Notification │ AuditLog           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│              Data Access Layer (Infrastructure)                 │
│             PensionVault.Infrastructure/Data                    │
├─────────────────────────────────────────────────────────────────┤
│         AppDbContext + Entity Framework Core                    │
│              Migrations │ Configuration                         │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    SQL Server Database                          │
│                     PensionVaultDb                              │
└─────────────────────────────────────────────────────────────────┘
```

## Module Interaction Flow

```
┌──────────────────────────────────────────────────────────────────┐
│                      USER JOURNEY                                │
└──────────────────────────────────────────────────────────────────┘

1. ONBOARDING FLOW
   ┌─────────────┐
   │  2.1 IAM    │  User Registration/Login
   └──────┬──────┘
          ↓
   ┌─────────────────────┐
   │ 2.2 Member Mgmt     │  Employer Registration
   │ 2.3 Contribution    │  Member Enrollment
   └──────┬──────────────┘
          ↓
   ┌──────────────────────┐
   │ 2.4 Ledger & Interest│  Fund Account Creation
   └──────┬───────────────┘
          ↓
   ┌──────────────────────────────────────┐
   │ 2.8 Notifications                    │
   │ → "Member enrolled successfully"     │
   └──────────────────────────────────────┘


2. CONTRIBUTION & CREDITING FLOW
   ┌─────────────────────────────┐
   │ 2.3 Contribution Processing │  Monthly Remittance
   │     (Employer submits)       │
   └──────────┬──────────────────┘
              ↓
   ┌──────────────────────────────────┐
   │ 2.4 Ledger Service              │
   │     (Post contributions)          │
   └──────────┬───────────────────────┘
              ↓
   ┌──────────────────────────┐
   │ 2.6 Investment & Corpus  │
   │     (Update fund values)  │
   └──────────┬───────────────┘
              ↓
   ┌──────────────────────────────────┐
   │ 2.4 Interest Credit (Annually)    │
   └──────────┬───────────────────────┘
              ↓
   ┌──────────────────────────────────┐
   │ 2.8 Notifications                │
   │ → "Interest credited: ₹X"        │
   └──────────────────────────────────┘


3. CLAIM & SETTLEMENT FLOW
   ┌──────────────────────┐
   │ 2.5 Claim Service    │  Member files claim
   │     (Submit claim)    │
   └──────────┬───────────┘
              ↓
   ┌──────────────────────┐
   │ 2.4 Ledger Service   │
   │ (Validate balance)   │
   └──────────┬───────────┘
              ↓
   ┌──────────────────────┐
   │ 2.5 Claim Service    │
   │ (Admin approves)     │
   └──────────┬───────────┘
              ↓
   ┌──────────────────────┐
   │ 2.7 Annuity Service  │
   │ (Create annuity or   │  If Retirement
   │  process settlement) │
   └──────────┬───────────┘
              ↓
   ┌──────────────────────────────────┐
   │ 2.8 Notifications                │
   │ → "Claim approved & disbursed"   │
   └──────────────────────────────────┘


4. PENSION DISBURSEMENT FLOW (Monthly)
   ┌──────────────────────────┐
   │ 2.7 Annuity Service      │
   │ (Calculate monthly       │
   │  pension amount)         │
   └──────────┬───────────────┘
              ↓
   ┌──────────────────────────────┐
   │ 2.4 Ledger Service           │
   │ (Post disbursement entry)    │
   └──────────┬──────────────────┘
              ↓
   ┌──────────────────────────────┐
   │ 2.8 Notifications            │
   │ → "Monthly pension: ₹Y"      │
   └──────────────────────────────┘


5. INVESTMENT MANAGEMENT FLOW (Monthly/Quarterly)
   ┌──────────────────────────────┐
   │ 2.6 Investment Service       │
   │ (Rebalance portfolio)        │
   └──────────┬───────────────────┘
              ↓
   ┌──────────────────────────────┐
   │ 2.6 Corpus Service           │
   │ (Update corpus records)      │
   └──────────┬───────────────────┘
              ↓
   ┌──────────────────────────────┐
   │ 2.8 Notifications            │
   │ → "Fund allocation updated"  │
   └──────────────────────────────┘


6. AUDIT & COMPLIANCE FLOW (Continuous)
   ┌────────────────────────────────┐
   │ 2.1 IAM - AuditLog             │
   │ (Track all user actions)       │
   └────────────────────────────────┘
   
   ┌────────────────────────────────┐
   │ 2.3 Contribution Service       │
   │ (Track employer compliance)    │
   └────────────────────────────────┘
   
   ┌────────────────────────────────┐
   │ 2.8 Notifications              │
   │ → Compliance reminders         │
   │ → Overdue contributions alert  │
   └────────────────────────────────┘
```

## Module Feature Matrix

| Feature | Module | Status | Priority |
|---------|--------|--------|----------|
| User Authentication | 2.1 | ✅ Complete | High |
| JWT Token Management | 2.1 | ✅ Complete | High |
| Audit Logging | 2.1 | ✅ Complete | High |
| Role-Based Access Control | 2.1 | ✅ Complete | High |
| Member Registration | 2.2 | ✅ Complete | High |
| Fund Account Creation | 2.2 | ✅ Complete | High |
| Employer Registration | 2.3 | ✅ Complete | High |
| Contribution Remittance | 2.3 | ✅ Complete | High |
| Reconciliation | 2.3 | 🔄 Partial | High |
| Default Tracking | 2.3 | 🔄 Partial | High |
| Ledger Management | 2.4 | ✅ Complete | High |
| Interest Crediting | 2.4 | ✅ Complete | High |
| Account Summary | 2.4 | ✅ Complete | Medium |
| Claim Filing | 2.5 | ✅ Complete | High |
| Claim Approval | 2.5 | ✅ Complete | High |
| Claim Disbursement | 2.5 | ✅ Complete | High |
| Partial Withdrawal | 2.5 | 🔄 Partial | Medium |
| Investment Allocation | 2.6 | ✅ Complete | High |
| Portfolio Tracking | 2.6 | ✅ Complete | High |
| Corpus Management | 2.6 | ✅ Complete | High |
| Annuity Plan Setup | 2.7 | ✅ Complete | High |
| Pension Disbursement | 2.7 | ✅ Complete | High |
| Nominee Settlement | 2.7 | 🔄 Partial | Medium |
| Notifications | 2.8 | ✅ Complete | High |
| Email Alerts | 2.8 | 🔄 Planned | Medium |
| SMS Alerts | 2.8 | 🔄 Planned | Medium |

Legend: ✅ Complete | 🔄 In Progress/Partial | 📋 Planned

## Technology Stack

### API Layer
- **Framework**: ASP.NET Core 10.0
- **Runtime**: .NET 10.0
- **API Style**: RESTful with Swagger/OpenAPI documentation

### Authentication & Security
- **JWT Bearer Tokens**: JWT token-based authentication
- **Password Hashing**: BCrypt for secure password storage
- **Authorization**: Role-Based Access Control (RBAC)

### Business Logic
- **ORM**: Entity Framework Core 10.0
- **Mapping**: AutoMapper for DTO conversions
- **Validation**: FluentValidation for request validation
- **Logging**: Serilog for structured logging

### Database
- **DBMS**: SQL Server
- **Migrations**: EF Core Code-First migrations
- **Backup**: Database backup recommended

### Development Tools
- **Version Control**: Git (GitHub)
- **CI/CD**: GitHub Actions (recommended)
- **Testing**: xUnit/NUnit (recommended)
- **Code Quality**: SonarQube (recommended)

## File Structure Summary

```
PensionVault-backend/
│
├── PensionVault.API/
│   ├── Controllers/           (11 controllers for each module)
│   ├── Middleware/            (Exception handling)
│   ├── Properties/            (App config)
│   ├── appsettings.json       (Configuration)
│   └── Program.cs             (Startup configuration)
│
├── PensionVault.Application/
│   ├── Services/              (9 services - business logic)
│   ├── DTOs/                  (Data transfer objects)
│   │   ├── Auth/
│   │   ├── Members/
│   │   ├── Employers/
│   │   ├── Contributions/
│   │   ├── Ledger/
│   │   ├── Claims/
│   │   ├── Investment/
│   │   ├── Annuity/
│   │   └── Misc/
│   └── IAppDbContext.cs       (Interface for DB context)
│
├── PensionVault.Domain/
│   ├── Entities/              (17 domain models)
│   └── Enums/                 (Business enumerations)
│
├── PensionVault.Infrastructure/
│   ├── Data/                  (DbContext & configuration)
│   ├── Migrations/            (Database schema changes)
│   └── Seeders/               (Initial data setup)
│
├── MODULE_MAPPING.md          (This comprehensive guide)
└── README.md                  (Project overview)
```

## Getting Started with Each Module

### To Work on Module 2.1 (IAM)
- Check: `AuthController.cs` → `AuthService.cs` → `User.cs`
- Update login/register logic in `AuthService.cs`
- Modify DTOs in `DTOs/Auth/`

### To Work on Module 2.2 (Member Management)
- Check: `MembersController.cs` → `MemberService.cs` → `Member.cs` & `FundAccount.cs`
- Implement new member features in `MemberService.cs`
- Update DTOs in `DTOs/Members/`

### To Work on Module 2.3 (Contributions)
- Check: `RemittancesController.cs` → `ContributionService.cs` & `EmployerService.cs`
- Update contribution logic in `ContributionService.cs`
- Modify DTOs in `DTOs/Contributions/`

### To Work on Module 2.4 (Ledger)
- Check: `LedgerController.cs` → `LedgerService.cs` → `LedgerEntry.cs`
- Implement ledger operations in `LedgerService.cs`
- Update DTOs in `DTOs/Ledger/`

### To Work on Module 2.5 (Claims)
- Check: `ClaimsController.cs` → `ClaimService.cs` → `BenefitClaim.cs`
- Update claim processing in `ClaimService.cs`
- Modify DTOs in `DTOs/Claims/`

### To Work on Module 2.6 (Investment)
- Check: `InvestmentController.cs` → `InvestmentService.cs` → `InvestmentPortfolio.cs`
- Update investment logic in `InvestmentService.cs`
- Modify DTOs in `DTOs/Investment/`

### To Work on Module 2.7 (Annuity)
- Check: `AnnuityController.cs` → `AnnuityService.cs` → `AnnuityPlan.cs`
- Implement annuity operations in `AnnuityService.cs`
- Update DTOs in `DTOs/Annuity/`

### To Work on Module 2.8 (Notifications)
- Check: `NotificationsController.cs` → Service triggers throughout
- Add notification calls in other services where needed
- Update DTOs in `DTOs/Misc/`

