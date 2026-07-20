# 🏦 PensionVault

**A Full-Featured Pension & Provident Fund Administration Platform**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=csharp)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver)
![JWT](https://img.shields.io/badge/Auth-JWT-000000?style=for-the-badge&logo=jsonwebtokens)
![Swagger](https://img.shields.io/badge/Docs-Swagger-85EA2D?style=for-the-badge&logo=swagger)

> A professional-grade REST API managing the complete financial lifecycle of a pension fund — from employer onboarding to final claim disbursement.

---

## 📑 Table of Contents

- [Overview](#overview)
- [Technology Stack](#technology-stack)
- [Solution Structure](#solution-structure)
- [Clean Architecture](#clean-architecture)
- [Domain Layer — Core Entities](#domain-layer--core-entities)
- [Application Layer — Business Services](#application-layer--business-services)
- [API Layer — Endpoints](#api-layer--endpoints)
- [Security — JWT and RBAC](#security--jwt-and-rbac)
- [The Financial Engine](#the-financial-engine)
- [The Claims Lifecycle](#the-claims-lifecycle)
- [Database Schema](#database-schema)
- [Configuration](#configuration)
- [Default Seed Data](#default-seed-data)
- [End-to-End Testing — Postman Guide](#end-to-end-testing--postman-guide)
- [Verifying Data in SSMS](#verifying-data-in-ssms)
- [RBAC Cheat Sheet](#rbac-cheat-sheet)
- [Common Errors and Fixes](#common-errors-and-fixes)

---

## 📋 Overview

PensionVault manages the complete financial lifecycle of a pension fund.

| Module | Functionality |
|--------|--------------|
| 🏢 **Employers** | Register companies, manage enrolled headcount |
| 👤 **Members** | Enrol employees, track profiles and retirement data |
| 💰 **Remittances** | Process monthly salary contributions with automatic ledger posting |
| 📒 **Ledger** | Double-entry, append-only financial ledger — every rupee tracked |
| 📝 **Claims** | Full claim workflow — submit → review → approve → disburse |
| 📈 **Investments** | Portfolio management across asset classes |
| 🏧 **Annuity** | Monthly pension disbursements post-retirement |
| 🔔 **Notifications** | System-wide messaging and compliance alerts |

---

## 🛠️ Technology Stack

| Concern | Technology | Reason |
|---------|-----------|--------|
| **Runtime** | ASP.NET Core 8 Web API | Fastest, modern .NET web framework |
| **Language** | C# 12 | Strongly typed, industry standard |
| **ORM** | Entity Framework Core 8 | C# classes instead of raw SQL |
| **Database** | SQL Server 2022 Express | Free, reliable, enterprise-grade RDBMS |
| **Authentication** | JWT Bearer Tokens | Stateless, secure, REST-standard |
| **Password Hashing** | BCrypt.Net | One-way hashing — no plain-text passwords |
| **Logging** | Serilog | Structured logging to file and console |
| **API Docs** | Swashbuckle (Swagger) | Auto-generated docs at `/swagger` |
| **CORS** | ASP.NET Core CORS | Allows frontend applications to call the API |

---

## 📁 Solution Structure

```
PensionVault/
│
├── PensionVault.Domain/                ← The Heart (zero dependencies)
│   ├── Entities/                       ← 17 C# classes → 17 SQL tables
│   │   ├── User.cs
│   │   ├── Member.cs
│   │   ├── Employer.cs
│   │   ├── FundAccount.cs
│   │   ├── FundScheme.cs
│   │   ├── LedgerEntry.cs
│   │   ├── MemberContribution.cs
│   │   ├── ContributionRemittance.cs
│   │   ├── BenefitClaim.cs
│   │   ├── ClaimDisbursement.cs
│   │   ├── InvestmentPortfolio.cs
│   │   ├── CorpusRecord.cs
│   │   ├── AnnuityPlan.cs
│   │   ├── MonthlyPensionDisbursement.cs
│   │   ├── InterestCreditRecord.cs
│   │   ├── Notification.cs
│   │   └── AuditLog.cs
│   └── Enums/
│       └── Enums.cs                    ← All 25 enums
│
├── PensionVault.Application/           ← The Brain (business logic)
│   └── Services/
│       ├── IAppDbContext.cs            ← Database interface (contract)
│       ├── AuthService.cs              ← Login, Register, JWT generation
│       ├── MemberService.cs            ← Member CRUD + account auto-creation
│       ├── EmployerService.cs          ← Company CRUD
│       ├── ContributionService.cs      ← Payroll remittances + ledger posting
│       ├── ClaimService.cs             ← Claim submit, approve, disburse
│       ├── LedgerService.cs            ← Interest, ledger summaries
│       ├── InvestmentService.cs        ← Portfolio management
│       └── AnnuityService.cs           ← Monthly pension payouts
│
├── PensionVault.Infrastructure/        ← The Plumbing (database access)
│   ├── Data/
│   │   └── AppDbContext.cs             ← EF Core context, 17 DbSets
│   ├── Configurations/                 ← Column configs, relationships
│   ├── Migrations/                     ← Auto-generated SQL migration scripts
│   └── Seeders/
│       └── DataSeeder.cs               ← Seeds default accounts on first run
│
└── PensionVault.API/                   ← The Face (HTTP layer)
    ├── Controllers/                    ← 11 controllers, 50+ endpoints
    │   ├── AuthController.cs
    │   ├── MembersController.cs
    │   ├── EmployersController.cs
    │   ├── RemittancesController.cs
    │   ├── ClaimsController.cs
    │   ├── LedgerController.cs
    │   ├── SchemesController.cs
    │   ├── InvestmentController.cs
    │   ├── AnnuityController.cs
    │   ├── NotificationsController.cs
    │   └── ReportsController.cs
    ├── Middleware/
    │   └── ExceptionMiddleware.cs      ← Global error handler → clean JSON
    ├── appsettings.json                ← DB connection string + JWT config
    └── Program.cs                      ← App startup, DI registrations
```

---

## 🏗️ Clean Architecture

PensionVault follows Clean Architecture — **dependencies always point inward**.

```
+--------------------------------------------------------+
|                    PensionVault.API                    |
|          (HTTP, Controllers, Swagger, Middleware)      |
|  +----------------------------------------------------+|
|  |              PensionVault.Application              ||
|  |         (Services, Business Logic, DTOs)           ||
|  |  +------------------------------------------------+||
|  |  |           PensionVault.Domain                  |||
|  |  |    (Entities, Enums - zero dependencies)       |||
|  |  +------------------------------------------------+||
|  +----------------------------------------------------+|
|  PensionVault.Infrastructure (EF Core, SQL Server)     |
+--------------------------------------------------------+
```

| Benefit | Explanation |
|---------|------------|
| **Testability** | Services can be unit tested with a mock DB — no real SQL Server needed |
| **Flexibility** | Swap SQL Server for PostgreSQL by changing only the Infrastructure layer |
| **Separation** | Business logic is completely isolated from HTTP and database concerns |
| **Clarity** | Every file has one clear responsibility |

---

## 🗂️ Domain Layer — Core Entities

### User

Stores every person who can log in. Passwords are **never** stored in plain text — BCrypt generates a one-way hash (`$2a$11$...`).

```csharp
public class User
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }          // Unique login identifier
    public string PasswordHash { get; set; }   // BCrypt hash only
    public UserRole Role { get; set; }         // Admin, Member, FundAdmin, etc.
    public UserStatus Status { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}
```

### Member

The pension-enrolled employee. Intentionally separate from `User` — authentication data and financial data are never mixed.

```csharp
public class Member
{
    public Guid MemberId { get; set; }
    public Guid UserId { get; set; }               // FK -> Users (login link)
    public string MembershipNumber { get; set; }   // e.g. "PV-2024-001"
    public string Name { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Guid EmployerId { get; set; }           // FK -> Employers
    public DateTime JoiningDate { get; set; }
    public DateTime? DateOfRetirement { get; set; }
    public string? NomineeDetails { get; set; }    // Stored as JSON
    public MemberStatus Status { get; set; }
}
```

### FundAccount

The money container for a member. **Auto-created on enrolment.** The `VestingPercent` controls how much of the balance the member can withdraw.

```csharp
public class FundAccount
{
    public Guid AccountId { get; set; }
    public Guid MemberId { get; set; }
    public Guid SchemeId { get; set; }
    public decimal EmployeeContributionBalance { get; set; }
    public decimal EmployerContributionBalance { get; set; }
    public decimal InterestAccrued { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal VestingPercent { get; set; }    // % eligible for withdrawal
    public FundAccountStatus Status { get; set; }
}
```

### LedgerEntry

Every single money movement, stored permanently. **Append-only** — nothing is ever deleted.

```csharp
public class LedgerEntry
{
    public Guid EntryId { get; set; }
    public Guid AccountId { get; set; }
    public EntryType EntryType { get; set; }   // ContributionCredit, ClaimDebit, etc.
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }  // Running balance
    public DateTime EntryDate { get; set; }
    public string? ReferenceId { get; set; }   // Links to Remittance or Claim ID
    public LedgerEntryStatus Status { get; set; }
}
```

### BenefitClaim

Created when a Member requests a withdrawal. Vested amount and TDS are **auto-calculated** on submission.

```csharp
public class BenefitClaim
{
    public Guid ClaimId { get; set; }
    public Guid MemberId { get; set; }
    public ClaimType ClaimType { get; set; }    // Retirement, Resignation, Housing, etc.
    public decimal EligibleAmount { get; set; }
    public decimal VestedAmount { get; set; }   // Auto: TotalBalance x VestingPercent
    public decimal TaxDeductible { get; set; }  // Auto: EligibleAmount x 10%
    public Guid? ProcessedById { get; set; }    // FK -> Users (audit trail)
    public ClaimStatus Status { get; set; }     // Submitted -> Approved -> Disbursed
}
```

### Enums Reference

| Enum | Values |
|------|--------|
| `UserRole` | Member, Employer, FundAdmin, InvestmentOfficer, Compliance, Admin |
| `ClaimType` | Retirement, Resignation, PartialWithdrawal, DeathClaim, Disability, Marriage, Housing |
| `ClaimStatus` | Submitted(0), UnderReview(1), Approved(2), Rejected(3), Disbursed(4) |
| `EntryType` | ContributionCredit, InterestCredit, PartialWithdrawal, TransferIn, TransferOut, ClaimDebit |
| `RemittanceStatus` | Received, Reconciled, Shortfall, Default |
| `SchemeType` | EPF, Gratuity, Superannuation, NPS, PPF |
| `AssetClass` | GovernmentSecurities, CorporateBonds, Equity, FixedDeposit, MoneyMarket |

---

## 🧠 Application Layer — Business Services

### AuthService — Identity Management

**`RegisterAsync`** — Validates email uniqueness → BCrypt hashes password → saves User → auto-issues JWT.

**`LoginAsync`** — Looks up User by email → `BCrypt.Verify(plainText, hash)` → checks `Active` status → issues tokens.

**`GenerateTokensAsync`** — The token engine:

1. Embeds claims into JWT: `sub` (UserID), `email`, `role`, `name`, `jti` (unique token ID)
2. Signs with `HmacSha256` using the secret from `appsettings.json`
3. Access token expires in **60 minutes**
4. Generates a 64-byte random **Refresh Token** valid for **7 days**
5. Persists Refresh Token on the User record

---

### MemberService — Enrolment Engine

**`CreateAsync`** — Precise creation sequence:

1. Validates `MembershipNumber` uniqueness
2. Creates and saves the `Member` entity
3. Increments `Employer.EnrolledMemberCount`
4. **Auto-creates a `FundAccount`** linked to the first available `FundScheme`
5. Commits everything in a single database transaction

---

### ContributionService — Payroll Engine

**`CreateRemittanceAsync`** — For each employee in the payroll batch:
- Creates a `MemberContribution` record
- Credits `EmployeeContributionBalance` and `EmployerContributionBalance` on the `FundAccount`
- Appends a `LedgerEntry` of type `ContributionCredit` with updated running balance
- All changes committed atomically

**`ReconcileAsync`** — Counts `Posted` contributions vs the employer's declared `CoverageCount`. Match → `Reconciled`. Mismatch → `Shortfall`.

---

### ClaimService — Payout Engine

**`SubmitClaimAsync`** — Auto-calculates on submission:
- `VestedAmount = TotalBalance x (VestingPercent / 100)`
- `TaxDeductible = EligibleAmount x 10%`

**`DisburseClaimAsync`** — Claim must be `Approved` first:

1. Creates `ClaimDisbursement` record
2. Calculates `NetAmount = DisbursedAmount - TaxDeducted`
3. Updates claim status to `Disbursed`
4. Subtracts `NetAmount` from `FundAccount.TotalBalance`
5. Appends a `LedgerEntry` of type `ClaimDebit`
6. Saves atomically

---

## 🔌 API Layer — Endpoints

### Auth (Public — no token required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/auth/register` | Create a new user account |
| `POST` | `/api/auth/login` | Login and receive JWT |
| `POST` | `/api/auth/refresh` | Renew token using refresh token |

**Login Response:**

```json
{
  "userId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "name": "Your Name",
  "email": "you@example.com",
  "role": "Admin",
  "token": "eyJhbGciOiJIUzI1NiIsInR...",
  "refreshToken": "base64encodedstring==",
  "tokenExpiry": "2026-05-30T06:00:00Z"
}
```

### Members

| Method | Endpoint | Roles | Description |
|--------|----------|-------|-------------|
| `POST` | `/api/members` | FundAdmin, Admin | Enroll a new member |
| `GET` | `/api/members` | FundAdmin, Admin | List all members |
| `GET` | `/api/members/{id}` | Member, FundAdmin, Admin | Get one member |
| `PUT` | `/api/members/{id}` | FundAdmin, Admin | Update member details |
| `GET` | `/api/members/{id}/accounts` | Member, FundAdmin | View fund accounts |
| `GET` | `/api/members/{id}/contributions` | Member, FundAdmin | View contributions |
| `GET` | `/api/members/{id}/ledger` | Member, FundAdmin | Full ledger history |
| `GET` | `/api/members/{id}/claims` | Member, FundAdmin | View all claims |

### Employers

| Method | Endpoint | Roles | Description |
|--------|----------|-------|-------------|
| `POST` | `/api/employers` | Admin, FundAdmin | Register a company |
| `GET` | `/api/employers` | Admin, FundAdmin, Compliance | List all companies |
| `GET` | `/api/employers/{id}` | Admin, FundAdmin, Compliance | Get one company |
| `PUT` | `/api/employers/{id}` | Admin, FundAdmin | Update company |

### Remittances

| Method | Endpoint | Roles | Description |
|--------|----------|-------|-------------|
| `POST` | `/api/remittances` | Employer, FundAdmin | Submit monthly payroll |
| `GET` | `/api/remittances/{id}` | Employer, FundAdmin, Admin | Get remittance details |
| `POST` | `/api/remittances/{id}/reconcile` | FundAdmin, Admin | Reconcile batch |
| `GET` | `/api/remittances/member/{id}` | Member, FundAdmin, Admin | Member contributions |

### Claims

| Method | Endpoint | Roles | Description |
|--------|----------|-------|-------------|
| `POST` | `/api/claims` | Member | Submit a withdrawal claim |
| `GET` | `/api/claims` | FundAdmin, Admin | List all claims |
| `GET` | `/api/claims/{id}` | Member, FundAdmin, Admin | Get specific claim |
| `PUT` | `/api/claims/{id}/review` | FundAdmin | Mark as Under Review |
| `PUT` | `/api/claims/{id}/approve` | FundAdmin | Approve the claim |
| `PUT` | `/api/claims/{id}/reject` | FundAdmin | Reject the claim |
| `POST` | `/api/claims/{id}/disburse` | FundAdmin | Disburse the approved claim |

---

## 🔐 Security — JWT and RBAC

### JWT Token Lifecycle

```
1.  POST /api/auth/login  (email + password)
         |
2.  BCrypt.Verify(plainText, storedHash)
         |
3.  Build JWT claims:
    |-- sub   -> UserGUID
    |-- email -> user@example.com
    |-- role  -> "FundAdmin"
    |-- name  -> "Fund Administrator"
    +-- exp   -> now + 60 minutes
         |
4.  Sign with HmacSha256 + secret key
         |
5.  Return: { token, refreshToken }
         |
6.  Client sends every request with:
    Authorization: Bearer eyJhbGci...
         |
7.  Middleware validates signature + expiry
    |-- Invalid --> 401 Unauthorized
    +-- Valid   --> extract role --> check RBAC
                   |-- Wrong role --> 403 Forbidden
                   +-- Correct   --> controller runs
```

### RBAC in Code

```csharp
[HttpPut("{id:guid}/approve")]
[Authorize(Roles = "FundAdmin")]
public async Task<IActionResult> Approve(Guid id) { ... }
```

A `Member` token calling this endpoint returns `403 Forbidden` immediately — the method body never executes.

### Why GUIDs for Primary Keys?

All primary keys are GUIDs (`941642b4-245e-4be0-ba18-59d313de5bf0`), not integers. This prevents:

- **ID enumeration attacks** — attackers cannot scrape by incrementing `/api/members/1`, `/api/members/2`
- **Business intelligence leakage** — sequential IDs reveal company size
- **Distributed system collisions** — GUIDs are globally unique across any number of servers

---

## 💸 The Financial Engine

### Remittance to Ledger Flow (Atomic)

```
POST /api/remittances
{
  employerId, remittancePeriod,
  members: [ { memberId, employeeAmt, employerAmt } ]
}
       |
       |-- Creates ContributionRemittance (batch header)
       |
       +-- For each member:
             |-- Creates MemberContribution record
             |-- FundAccount.EmployeeContributionBalance += employeeAmt
             |-- FundAccount.EmployerContributionBalance += employerAmt
             |-- FundAccount.TotalBalance += total
             +-- Creates LedgerEntry:
                   EntryType    = ContributionCredit
                   Amount       = total
                   BalanceAfter = new TotalBalance
                   ReferenceId  = RemittanceId
```

**One remittance produces per employee:**

- 1 row in `ContributionRemittances`
- 1 row in `MemberContributions`
- 1 row in `LedgerEntries`
- Updated `FundAccounts.TotalBalance`

---

## 📋 The Claims Lifecycle

```
Member submits claim
       |
       v
[0] Submitted
    Auto-calculates:
    |-- VestedAmount = TotalBalance x (VestingPercent / 100)
    +-- TaxDeductible = EligibleAmount x 10%
       |
       v  (FundAdmin reviews)
[1] UnderReview  (optional)
       |
       v  (FundAdmin approves)
[2] Approved  -- ProcessedById recorded for full audit trail
       |
       |---> (FundAdmin disburses)
       |    [4] Disbursed
       |         |-- ClaimDisbursement row created
       |         |-- LedgerEntry (ClaimDebit) appended
       |         +-- FundAccount.TotalBalance -= NetAmount
       |
       +---> (FundAdmin rejects)
            [3] Rejected  (no money moves)
```

**TDS Calculation Example:**

| Field | Calculation | Amount |
|-------|------------|--------|
| Eligible Amount | (requested) | Rs. 50,000 |
| Tax Deductible | Rs. 50,000 x 10% | Rs. 5,000 |
| Net Disbursed | Rs. 50,000 - Rs. 5,000 | **Rs. 45,000** |

---

## 🗄️ Database Schema

### Table Relationships

```
dbo.Users
    |
    +-- dbo.Members
    |       +-- dbo.FundAccounts
    |       |       +-- dbo.LedgerEntries
    |       |       +-- dbo.InterestCreditRecords
    |       +-- dbo.MemberContributions
    |       +-- dbo.BenefitClaims
    |       |       +-- dbo.ClaimDisbursements
    |       +-- dbo.AnnuityPlans
    |               +-- dbo.MonthlyPensionDisbursements
    |
    +-- dbo.Employers
            +-- dbo.ContributionRemittances

dbo.FundSchemes
    +-- dbo.FundAccounts
    +-- dbo.InvestmentPortfolios
            +-- dbo.CorpusRecords

dbo.Notifications
dbo.AuditLogs
```

### Key Foreign Key Constraints

| Child Table | Column | Parent | Rule |
|-------------|--------|--------|------|
| `Members` | `UserId` | `Users` | Member must have a login account |
| `Members` | `EmployerId` | `Employers` | Member must belong to a company |
| `FundAccounts` | `MemberId` | `Members` | Account must belong to a member |
| `LedgerEntries` | `AccountId` | `FundAccounts` | Every movement needs an account |
| `BenefitClaims` | `MemberId` | `Members` | Claim must belong to a member |
| `ClaimDisbursements` | `ClaimId` | `BenefitClaims` | Disbursement requires an approved claim |
| `MemberContributions` | `RemittanceId` | `ContributionRemittances` | Contribution is part of a payroll batch |

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=PensionVaultDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key":           "your-256-bit-secret-key-here",
    "Issuer":        "PensionVault",
    "Audience":      "PensionVaultUsers",
    "ExpireMinutes": "60"
  }
}
```

### Startup Sequence (Program.cs)

```
1.  Serilog           -> Structured logging (file + console)
2.  SQL Server        -> Connect via EF Core
3.  DI Registration   -> All services and interfaces
4.  JWT Auth          -> Token validation parameters
5.  Swagger           -> API documentation with auth support
6.  CORS              -> Allow frontend origins
--- App starts ---
7.  MigrateAsync()    -> Apply any pending EF migrations
8.  SeedAsync()       -> Insert default users and schemes
--- Middleware pipeline (order is critical) ---
9.  ExceptionMiddleware -> Global error handler (must be first)
10. Serilog Request Logging
11. CORS
12. UseAuthentication  -> Must come before Authorization
13. UseAuthorization
14. MapControllers
```

---

## 🌱 Default Seed Data

On first run, the following are automatically inserted.

> **Warning:** Change all default passwords before deploying to production.

### User Accounts

| Name | Email | Password | Role |
|------|-------|----------|------|
| System Administrator | `admin@pensionvault.com` | `Admin@123` | Admin |
| Fund Administrator | `fundadmin@pensionvault.com` | `FundAdmin@123` | FundAdmin |
| Sample Employer | `employer@pensionvault.com` | `Employer@123` | Employer |
| Compliance Officer | `compliance@pensionvault.com` | `Compliance@123` | Compliance |

### Fund Schemes

| Scheme | Type | Interest Rate |
|--------|------|--------------|
| Employee Provident Fund | EPF | 8.15% |
| National Pension System | NPS | 9.00% |
| Gratuity Fund | Gratuity | 7.50% |
| Superannuation Fund | Superannuation | 8.00% |

### Sample Employer

| Company | Registration | Industry |
|---------|-------------|----------|
| Acme Technologies Pvt Ltd | CIN-ACME-2024-001 | Information Technology |

---

## 🧪 End-to-End Testing — Postman Guide

**Before you start:**
- Run the API: `dotnet run --project PensionVault.API --urls "http://localhost:5000"`
- All steps after Step 2 require a JWT in Postman under `Authorization > Bearer Token`
- All request bodies use `Body > raw > JSON` in Postman
- All IDs are GUIDs — always copy from your actual responses, never type them manually

---

### Scenario A — System Setup

#### Step 1 — Register Admin Account

```
POST http://localhost:5000/api/auth/register
```

```json
{
  "name": "Your Full Name",
  "email": "your.email@company.com",
  "password": "SecurePassword123!",
  "role": "Admin"
}
```

Expected: `201 Created` — copy the `token` from the response.

---

#### Step 2 — Login as Admin

```
POST http://localhost:5000/api/auth/login
```

```json
{
  "email": "your.email@company.com",
  "password": "SecurePassword123!"
}
```

Expected: `200 OK` — copy the `token`, paste into Postman `Authorization > Bearer Token`.

---

#### Step 3 — Register a Company

```
POST http://localhost:5000/api/employers
Authorization: Bearer <admin_token>
```

```json
{
  "companyName": "Your Company Name",
  "registrationNumber": "CIN-YOUR-001",
  "industry": "Information Technology",
  "remittanceFrequency": 1,
  "contactDetails": "hr@yourcompany.com",
  "status": 1
}
```

Expected: `201 Created` — copy the `employerId`.

---

### Scenario B — Member Enrolment

#### Step 4 — Create Employee Login Account

```
POST http://localhost:5000/api/auth/register
```

```json
{
  "name": "Employee Full Name",
  "email": "employee@yourcompany.com",
  "password": "EmployeePass123!",
  "role": "Member"
}
```

Expected: `201 Created` — copy the `userId`.

---

#### Step 5 — Enroll Employee in the Pension Fund

```
POST http://localhost:5000/api/members
Authorization: Bearer <admin_token>
```

```json
{
  "userId": "<PASTE_USER_ID>",
  "membershipNumber": "EMP-2026-001",
  "name": "Employee Full Name",
  "dateOfBirth": "1995-06-15T00:00:00Z",
  "gender": "Male",
  "nationalIdRef": "AADHAR-1234-5678",
  "employerId": "<PASTE_EMPLOYER_ID>",
  "joiningDate": "2024-01-01T00:00:00Z",
  "dateOfRetirement": "2055-06-15T00:00:00Z",
  "nomineeDetails": "Spouse Name",
  "status": 1
}
```

Expected: `201 Created` — copy the `memberId`. A Fund Account was **automatically created** behind the scenes.

---

### Scenario C — Payroll Submission

#### Step 6 — Submit Monthly Payroll

> Login as FundAdmin first: `fundadmin@pensionvault.com` / `FundAdmin@123` — copy that token.

```
POST http://localhost:5000/api/remittances
Authorization: Bearer <fundadmin_token>
```

```json
{
  "employerId": "<PASTE_EMPLOYER_ID>",
  "remittancePeriod": "2026-05",
  "totalEmployeeShare": 5000,
  "totalEmployerShare": 5000,
  "coverageCount": 1,
  "memberContributions": [
    {
      "memberId": "<PASTE_MEMBER_ID>",
      "employeeAmount": 5000,
      "employerAmount": 5000
    }
  ]
}
```

Expected: `201 Created` — database now contains 1 remittance + 1 contribution + 1 ledger entry + Rs. 10,000 balance.

---

#### Step 7 — Verify the Ledger

```
GET http://localhost:5000/api/members/<MEMBER_ID>/ledger
Authorization: Bearer <fundadmin_token>
```

Expected `200 OK`:

```json
[
  {
    "entryType": "ContributionCredit",
    "amount": 10000.00,
    "balanceAfter": 10000.00,
    "status": "Posted"
  }
]
```

---

### Scenario D — Claims and Disbursement

#### Step 8 — Employee Logs In

```
POST http://localhost:5000/api/auth/login
```

```json
{
  "email": "employee@yourcompany.com",
  "password": "EmployeePass123!"
}
```

Copy the member token and put it in the Authorization header.

---

#### Step 9 — Employee Submits a Claim

```
POST http://localhost:5000/api/claims
Authorization: Bearer <member_token>
```

```json
{
  "memberId": "<PASTE_MEMBER_ID>",
  "claimType": 2,
  "eligibleAmount": 5000,
  "reason": "Partial withdrawal for home renovation"
}
```

Expected: `201 Created` — note the auto-calculated fields `vestedAmount: 10000.00` and `taxDeductible: 500.00`. Copy the `claimId`.

---

#### Step 10 — FundAdmin Approves the Claim

```
PUT http://localhost:5000/api/claims/<CLAIM_ID>/approve
Authorization: Bearer <fundadmin_token>
```

No request body needed.

Expected: `200 OK` — `status: 2` (Approved), `processedByName` recorded for the audit trail.

---

#### Step 11 — FundAdmin Disburses the Money

```
POST http://localhost:5000/api/claims/<CLAIM_ID>/disburse
Authorization: Bearer <fundadmin_token>
```

```json
{
  "disbursedAmount": 5000,
  "taxDeducted": 500,
  "bankAccountRef": "HDFC-SAVINGS-123456"
}
```

Expected `200 OK`:

```json
{
  "disbursedAmount": 5000.00,
  "taxDeducted": 500.00,
  "netAmount": 4500.00,
  "bankAccountRef": "HDFC-SAVINGS-123456",
  "status": 1
}
```

---

#### Step 12 — Final Ledger Audit

```
GET http://localhost:5000/api/members/<MEMBER_ID>/ledger
```

Expected `200 OK` — two entries proving the complete financial trail:

```json
[
  {
    "entryType": "ClaimDebit",
    "amount": 4500.00,
    "balanceAfter": 5500.00
  },
  {
    "entryType": "ContributionCredit",
    "amount": 10000.00,
    "balanceAfter": 10000.00
  }
]
```

Rs. 10,000 deposited → Rs. 4,500 withdrawn (net of tax) → Rs. 5,500 remaining.

---

## 🔍 Verifying Data in SSMS

**Connect:** Server `.\SQLEXPRESS` → Windows Authentication → Trust Server Certificate → expand `PensionVaultDb > Tables`

```sql
-- All users
SELECT UserId, Name, Email, Role, Status FROM Users

-- Members with their employer
SELECT m.Name, m.MembershipNumber, e.CompanyName, m.JoiningDate
FROM Members m
JOIN Employers e ON m.EmployerId = e.EmployerId

-- Ledger for a specific member
SELECT le.EntryType, le.Amount, le.BalanceAfter, le.EntryDate
FROM LedgerEntries le
JOIN FundAccounts fa ON le.AccountId = fa.AccountId
JOIN Members m ON fa.MemberId = m.MemberId
WHERE m.Name = 'Employee Full Name'
ORDER BY le.EntryDate DESC

-- All claims with approver info
SELECT m.Name AS MemberName, bc.ClaimType, bc.VestedAmount,
       bc.Status, u.Name AS ApprovedBy
FROM BenefitClaims bc
JOIN Members m ON bc.MemberId = m.MemberId
LEFT JOIN Users u ON bc.ProcessedById = u.UserId

-- All disbursements
SELECT m.Name, cd.DisbursedAmount, cd.TaxDeducted,
       cd.NetAmount, cd.BankAccountRef, cd.DisbursedDate
FROM ClaimDisbursements cd
JOIN Members m ON cd.MemberId = m.MemberId
ORDER BY cd.DisbursedDate DESC
```

---

## 📊 RBAC Cheat Sheet

| Action | Admin | FundAdmin | Member | Employer | Compliance |
|--------|:-----:|:---------:|:------:|:--------:|:----------:|
| Register / Login | ✅ | ✅ | ✅ | ✅ | ✅ |
| Create Employer | ✅ | ✅ | ❌ | ❌ | ❌ |
| View All Employers | ✅ | ✅ | ❌ | ❌ | ✅ |
| Enroll Member | ✅ | ✅ | ❌ | ❌ | ❌ |
| View All Members | ✅ | ✅ | ❌ | ❌ | ❌ |
| View Own Profile | ✅ | ✅ | ✅ | ❌ | ❌ |
| Submit Remittance | ❌ | ✅ | ❌ | ✅ | ❌ |
| Reconcile Remittance | ✅ | ✅ | ❌ | ❌ | ❌ |
| Submit Claim | ❌ | ❌ | ✅ | ❌ | ❌ |
| View All Claims | ✅ | ✅ | ❌ | ❌ | ✅ |
| Approve / Reject Claim | ❌ | ✅ | ❌ | ❌ | ❌ |
| Disburse Claim | ❌ | ✅ | ❌ | ❌ | ❌ |
| View Own Ledger | ✅ | ✅ | ✅ | ❌ | ❌ |
| View Reports | ✅ | ✅ | ❌ | ❌ | ✅ |
| View Notifications | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## ❌ Common Errors and Fixes

| Code | Message | Cause | Fix |
|------|---------|-------|-----|
| `400` | `"Email already registered"` | Duplicate email in Users | Use a different email |
| `400` | `"Membership number already exists"` | Duplicate `MembershipNumber` | Use a unique membership number |
| `401` | *(empty body)* | No token provided | Add `Authorization: Bearer <token>` header |
| `401` | `"Invalid email or password"` | Wrong credentials | Check email and password |
| `403` | *(empty body)* | Token valid, but wrong role | Login with the correct role for this endpoint |
| `415` | *(empty body)* | Body not set to JSON | Postman: `Body > raw > JSON` |
| `500` | `"FK constraint violated"` | Referencing a non-existent `UserId` | Register the user first, then use the returned ID |
| `500` | `"Claim must be approved"` | Disbursing a non-approved claim | Call `/approve` before `/disburse` |

---

## ✅ Backend Phase 1 — Complete

All financial workflows — from employer onboarding to final claim disbursement — are production-ready.

*Next phase: React Frontend Web Application*

---

## 📝 License

This project is proprietary and confidential.

## 👥 Contributors

- **PRASAD005-BOT** - Lead Developer
