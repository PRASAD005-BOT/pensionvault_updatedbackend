# PensionVault — Error Handling & Service Structure Refactor

**Date:** 2026-07-25
**Scope:** Backend (`PensionVault_Backend`) — Members service + `PensionVault.Shared`, plus a structural change across all four service projects.
**Status:** Implemented and building green. A few optional follow-ups remain (see §7).

---

## 1. Background / Problem

The codebase used **exceptions as control flow**. Service methods threw `KeyNotFoundException`,
`UnauthorizedAccessException`, `InvalidOperationException`, etc. for *expected* outcomes (a missing
member profile, a failed login), and a shared `ExceptionMiddleware` caught them and mapped them to
HTTP status codes.

Functionally this worked, but it produced two problems during development:

1. **The Visual Studio debugger broke on every such throw** ("Break when exception is user-unhandled"),
   forcing *Continue* on nearly every action — because these exceptions fire constantly (profile
   lookups on every page load, failed logins during testing).
2. **Console log noise** from the same paths.

There were ~108 `throw` sites across 11 files. The fix was **not** "add more middleware" (the
middleware already worked; the debugger breaks at the `throw`, before any `catch`). The fix was to
stop throwing for *expected* results.

---

## 2. Guiding principle

> **Exceptions are for *exceptional* conditions — not for expected, predictable outcomes.**

Throws were triaged into three categories, each with a distinct treatment:

| Category | Example | Treatment |
|---|---|---|
| **A. Expected "not found" reads** (fire on normal navigation) | member/employer profile lookup | Return `null` -> controller returns `NotFound()` |
| **B. Auth/validation failures with a message + status** | invalid password, email already registered | Return `ServiceResult<T>` -> controller maps status + message |
| **C. Genuinely exceptional / action guards** (fire only on invalid input) | "Claim not found" during a *disburse* | **Left as-is** — thrown and mapped by middleware |

---

## 3. Category A — Nullable "not found" reads

Expected misses now return `null`; the controller owns the HTTP outcome.

**Pattern:**
```csharp
// Service
public async Task<MemberResponse?> GetByUserIdAsync(Guid userId)
{
    var member = await _memberRepo.FindByUserIdAsync(userId);
    return member is null ? null : ToResponse(member);
}

// Controller
var member = await _memberService.GetByUserIdAsync(userId);
return member is null ? NotFound() : Ok(member);
```

**Files changed:**
- `Members.Services/Services/MemberService.cs` / `.../Interfaces/IMemberService.cs` —
  `GetByUserIdAsync` -> nullable.
- `Members.API/Controllers/MembersController.cs` — 4 call sites updated (`GetAll` member branch,
  `me`, `Update`, `by-user`); **two pre-existing `try/catch` workarounds removed** (they existed only
  to swallow the throw).
- `Members.Services/Services/EmployerService.cs` / `.../Interfaces/IEmployerService.cs` —
  `GetByIdAsync` + `GetByUserIdAsync` -> nullable.
- `Members.API/Controllers/EmployersController.cs` — `GetById` + `me` return `NotFound()` on null.

**Not converted:** `MemberService.GetByIdAsync` — it is reused internally as a *reload-after-write*
(after Create/Update/Approve/SelfEnroll) where the entity is guaranteed to exist; a miss there is a
genuine invariant violation, so it correctly still throws.

---

## 4. Category B — `ServiceResult<T>` for auth/validation

A `null` cannot convey *why* a login failed or *which* status to return, so these use an explicit
result object.

**New type:** `PensionVault.Shared/Results/ServiceResult.cs`
```csharp
public record ServiceResult<T>
{
    public bool Success { get; }
    public T? Value { get; }
    public string? Error { get; }
    public int StatusCode { get; }
    public static ServiceResult<T> Ok(T value) => new(true, value, null, 200);
    public static ServiceResult<T> Fail(string error, int statusCode) => new(false, default, error, statusCode);
}
```

**Applied to:** `Members.Services/Services/AuthService.cs` — `LoginAsync`, `RegisterAsync`,
`RefreshTokenAsync`. All 9 throws became returns:

| Condition | Before | After |
|---|---|---|
| Invalid credentials / role / inactive / pending / deregistered | `throw new UnauthorizedAccessException(msg)` | `return ServiceResult<AuthResponse>.Fail(msg, 401)` |
| Email already registered | `throw new InvalidOperationException(...)` | `Fail("Email already registered.", 409)` |
| Invalid role string | `throw new ArgumentException(...)` | `Fail("Invalid role specified.", 400)` |
| Success | `return authResponse` | `return ServiceResult<AuthResponse>.Ok(authResponse)` |

**Controller mapping** (`Members.API/Controllers/AuthController.cs`):
```csharp
var result = await _authService.LoginAsync(request);
return result.Success
    ? Ok(result.Value)
    : StatusCode(result.StatusCode, new { message = result.Error, error = result.Error });
```

**Interface:** `.../Interfaces/IAuthService.cs` return types updated to `Task<ServiceResult<AuthResponse>>`.

---

## 5. Response-contract compatibility

- **Success responses are byte-identical** to before (the controller returns `result.Value`, which is
  the same `AuthResponse`/`MemberResponse` payload).
- **Error bodies include both `message` and `error`** so the existing frontend (`Login.tsx` reads
  `data.error || data.message`, other pages read `data.message`) displays messages unchanged.
- HTTP status codes are preserved (401/400/409/404 as before).

No frontend changes were required.

---

## 6. Interface restructure

Each service project's `Services/` folder now has an `Interfaces/` subfolder holding the interface
files; implementations stay in `Services/`.

```
*.Services/Services/
├── Interfaces/          <- I<Name>Service.cs
└── <Name>Service.cs     <- implementations
```

Applied to all four services:

| Service | Interfaces/ | Services/ (impl) |
|---|---|---|
| Contributions | IContributionService, IInvestmentService, ILedgerService, IReportService | ContributionService, InvestmentService, LedgerService, ReportService |
| Members | IAuthService, IEmployerService, IMemberService, INotificationService, ISchemeService, IUserService | Auth, Employer, Member, Notification, Scheme, User |
| Claims | IClaimService | ClaimService |
| Annuity | IAnnuityService | AnnuityService |

**Why it's non-breaking:**
- **Namespaces were intentionally left unchanged** (e.g. `IContributionService` remains in
  `namespace Contributions.Services`). In C#, folder != namespace, so no `using`/DI/reference changes
  were needed.
- Projects are **SDK-style** (`<Project Sdk="Microsoft.NET.Sdk">`) with **default `**/*.cs` globbing**
  and no explicit `<Compile Include>` — moved files are still compiled automatically; **no `.csproj`
  edits.**

**Expected cosmetic note:** VS may show suggestion-level hint `IDE0130: Namespace does not match
folder structure` on the moved files. This is intentional and harmless (not a warning/error); it can
be silenced via `.editorconfig` if desired.

---

## 7. Verification

- `dotnet build` on the **Members service** was green (0 warnings / 0 errors) after the Category A and
  Category B changes.
- `dotnet build PensionVault.sln` was green after the auth refactor.
- The interface move is **compile-invariant** (namespaces unchanged + SDK globbing) and was verified
  by inspection after the last green solution build.

> Builds were run to a scratch output directory to avoid file locks from the running debug processes.
> To see it locally: **Stop debugging -> Rebuild -> Run** in Visual Studio.

---

## 8. Open recommendations (not yet applied)

These were identified but deliberately left for a later decision:

1. **Category C throws + debugger:** the remaining `throw`s in Claims/Contributions/Annuity are action
   guards (`Submit`/`Approve`/`Disburse`/`Reconcile`...). They fire only on invalid input and are
   correctly mapped by middleware. Rather than convert them, silence the behavior once in
   **Debug -> Windows -> Exception Settings -> Common Language Runtime Exceptions** (untick
   `KeyNotFoundException`, etc.). Solution-wide, zero code.
2. **Log noise:** raise `Polly` / `Microsoft.Extensions.Http.Resilience` to `Warning` in each service's
   Serilog config to remove the `Execution attempt` telemetry lines.
3. **Persistent logs:** logging is **console-only** (no file sink) and the **Gateway isn't wired to
   Serilog**. Add `Serilog.Sinks.File` (and optionally `UseSerilogRequestLogging()` +
   `Log.CloseAndFlush()`) if durable/searchable logs are wanted.

---

## 9. Conventions established (for future work)

- **Expected miss -> `null` -> `NotFound()`.** Don't throw for "not found" on read endpoints.
- **Failure needing a message/status -> `ServiceResult<T>`.** Reuse the Shared type; don't throw for
  validation/auth.
- **Throw only for the genuinely exceptional** (broken invariants, corrupt data, unreachable
  dependency) and let `ExceptionMiddleware` map it.
- **Interfaces live in `Services/Interfaces/`**, namespace stays the service namespace.