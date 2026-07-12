using PensionVault.Application.DTOs.Annuity;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Application.Services;

public class AnnuityService : IAnnuityService
{
    // Eligibility thresholds (EPS rules, India)
    private const int MinServiceYears         = 10;   // Minimum contributory service years
    private const int FullPensionAgeYears     = 58;   // Full pension eligibility age
    private const int EarlyPensionAgeYears    = 50;   // Early pension eligibility age

    private readonly IAnnuityRepository        _annuityRepo;
    private readonly IAnnuityRequestRepository _requestRepo;
    private readonly IFundAccountRepository    _accountRepo;
    private readonly ILedgerRepository         _ledgerRepo;
    private readonly IMemberRepository         _memberRepo;
    private readonly IContributionRepository   _contributionRepo;
    private readonly INotificationRepository   _notificationRepo;
    private readonly IUnitOfWork               _unitOfWork;

    public AnnuityService(
        IAnnuityRepository        annuityRepo,
        IAnnuityRequestRepository requestRepo,
        IFundAccountRepository    accountRepo,
        ILedgerRepository         ledgerRepo,
        IMemberRepository         memberRepo,
        IContributionRepository   contributionRepo,
        INotificationRepository   notificationRepo,
        IUnitOfWork               unitOfWork)
    {
        _annuityRepo      = annuityRepo;
        _requestRepo      = requestRepo;
        _accountRepo      = accountRepo;
        _ledgerRepo       = ledgerRepo;
        _memberRepo       = memberRepo;
        _contributionRepo = contributionRepo;
        _notificationRepo = notificationRepo;
        _unitOfWork       = unitOfWork;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Eligibility
    // ══════════════════════════════════════════════════════════════════════════

    public async Task<AnnuityEligibilityResponse> CheckEligibilityAsync(Guid memberId)
    {
        var member = await _memberRepo.FindByIdAsync(memberId)
            ?? throw new KeyNotFoundException("Member not found.");

        // Age
        var today      = DateTime.UtcNow.Date;
        var dob        = member.DateOfBirth.Date;
        int ageYears   = today.Year - dob.Year;
        if (dob.AddYears(ageYears) > today) ageYears--;

        // Service length
        var joinDate      = member.JoiningDate.Date;
        int serviceYears  = today.Year - joinDate.Year;
        if (joinDate.AddYears(serviceYears) > today) serviceYears--;

        // Contribution months
        var contributions = await _contributionRepo.GetByMemberAsync(memberId);
        var distinctMonths = contributions
            .Select(c => c.Period)
            .Distinct()
            .Count();

        // Pension balance
        var accounts      = await _accountRepo.GetByMemberAsync(memberId);
        var pensionBalance = accounts.Sum(a => a.PensionBalance);

        var failures = new List<string>();

        if (ageYears < EarlyPensionAgeYears)
            failures.Add($"Age must be at least {EarlyPensionAgeYears} years (current: {ageYears}).");

        if (serviceYears < MinServiceYears)
            failures.Add($"Minimum {MinServiceYears} years of service required (current: {serviceYears}).");

        if (pensionBalance <= 0)
            failures.Add("Pension (EPS) balance must be greater than ₹0.");

        if (member.Status != MemberStatus.Active && member.Status != MemberStatus.Retired)
            failures.Add($"Member status must be Active or Retired (current: {member.Status}).");

        return new AnnuityEligibilityResponse(
            memberId,
            IsEligible: failures.Count == 0,
            AgeYears: ageYears,
            ServiceYears: serviceYears,
            ContributionMonths: distinctMonths,
            PensionBalance: pensionBalance,
            FailureReasons: failures);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Annuity Requests (DB-persisted)
    // ══════════════════════════════════════════════════════════════════════════

    public async Task<AnnuityRequestResponse> SubmitAnnuityRequestAsync(SubmitAnnuityRequestDto dto)
    {
        // Eligibility gate
        var eligibility = await CheckEligibilityAsync(dto.MemberId);
        if (!eligibility.IsEligible)
            throw new InvalidOperationException(
                "Member is not eligible for annuity: " +
                string.Join("; ", eligibility.FailureReasons));

        // Only one pending request allowed at a time
        var existing = await _requestRepo.FindPendingByMemberAsync(dto.MemberId);
        if (existing != null)
            throw new InvalidOperationException("A pending annuity request already exists. Cancel it first.");

        // Calculate estimated monthly pension (8% annual rate standard)
        decimal monthly = CalcMonthlyPension(eligibility.PensionBalance, dto.PlanType);

        var request = new AnnuityRequest
        {
            MemberId               = dto.MemberId,
            PlanType               = dto.PlanType,
            PensionBalanceAtRequest = eligibility.PensionBalance,
            EstimatedMonthly       = Math.Round(monthly, 2),
            Note                   = dto.Note,
            Status                 = AnnuityRequestStatus.Pending,
            RequestedAt            = DateTime.UtcNow,
        };
        await _requestRepo.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

        return await BuildRequestResponseAsync(request);
    }

    public async Task<IEnumerable<AnnuityRequestResponse>> GetPendingRequestsAsync()
    {
        var requests = await _requestRepo.GetPendingAsync();
        var tasks    = requests.Select(r => BuildRequestResponseAsync(r));
        return await Task.WhenAll(tasks);
    }

    public async Task<IEnumerable<AnnuityRequestResponse>> GetMemberRequestsAsync(Guid memberId)
    {
        var requests = await _requestRepo.GetByMemberAsync(memberId);
        var tasks    = requests.Select(r => BuildRequestResponseAsync(r));
        return await Task.WhenAll(tasks);
    }

    public async Task<AnnuityRequestResponse> ApproveRequestAsync(Guid requestId, Guid reviewerUserId)
    {
        var request = await _requestRepo.FindByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Annuity request not found.");

        if (request.Status != AnnuityRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be approved.");

        // Re-check eligibility at approval time
        var eligibility = await CheckEligibilityAsync(request.MemberId);
        if (!eligibility.IsEligible)
            throw new InvalidOperationException(
                "Member no longer meets eligibility criteria: " +
                string.Join("; ", eligibility.FailureReasons));

        // Use live pension balance at approval time
        var livePensionBalance = eligibility.PensionBalance;
        if (livePensionBalance <= 0)
            throw new InvalidOperationException("Pension balance is zero. Cannot create annuity plan.");

        var member = await _memberRepo.FindByIdAsync(request.MemberId);

        var monthly = CalcMonthlyPension(livePensionBalance, request.PlanType);

        // Create the annuity plan — purchase value comes from pension balance ONLY
        var plan = new AnnuityPlan
        {
            MemberId         = request.MemberId,
            PlanType         = request.PlanType,
            PurchaseValue    = livePensionBalance,
            MonthlyPension   = Math.Round(monthly, 2),
            AnnuityStartDate = DateTime.UtcNow,
            NomineeDetails   = member?.NomineeDetails ?? request.Note,
            Status           = AnnuityStatus.Active
        };
        await _annuityRepo.AddAsync(plan);

        // Mark request approved
        request.Status          = AnnuityRequestStatus.Approved;
        request.ReviewedAt      = DateTime.UtcNow;
        request.ReviewedByUserId = reviewerUserId;

        // Notify member
        if (member != null)
        {
            await _notificationRepo.AddAsync(new Notification
            {
                UserId      = member.UserId,
                Message     = $"Your annuity request ({request.PlanType}) has been approved. Monthly pension: ₹{plan.MonthlyPension:N2}.",
                Category    = NotificationCategory.Annuity,
                Status      = NotificationStatus.Unread,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return await BuildRequestResponseAsync(request);
    }

    public async Task<AnnuityRequestResponse> RejectRequestAsync(Guid requestId, Guid reviewerUserId, string? reviewNote)
    {
        var request = await _requestRepo.FindByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Annuity request not found.");

        if (request.Status != AnnuityRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be rejected.");

        request.Status           = AnnuityRequestStatus.Rejected;
        request.ReviewedAt       = DateTime.UtcNow;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewNote       = reviewNote;

        var member = await _memberRepo.FindByIdAsync(request.MemberId);
        if (member != null)
        {
            await _notificationRepo.AddAsync(new Notification
            {
                UserId      = member.UserId,
                Message     = $"Your annuity request has been rejected. Reason: {reviewNote ?? "No reason provided."}",
                Category    = NotificationCategory.Annuity,
                Status      = NotificationStatus.Unread,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return await BuildRequestResponseAsync(request);
    }

    public async Task<AnnuityRequestResponse> CancelRequestAsync(Guid requestId, Guid memberId)
    {
        var request = await _requestRepo.FindByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Annuity request not found.");

        if (request.MemberId != memberId)
            throw new UnauthorizedAccessException("You can only cancel your own requests.");

        if (request.Status != AnnuityRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be cancelled.");

        request.Status     = AnnuityRequestStatus.Cancelled;
        request.ReviewedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return await BuildRequestResponseAsync(request);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Approved Annuity Plans
    // ══════════════════════════════════════════════════════════════════════════

    public async Task<AnnuityResponse> CreateAnnuityAsync(CreateAnnuityRequest request)
    {
        if (request.PurchaseValue <= 0)
            throw new ArgumentException("Purchase value must be greater than zero.");
        if (request.MonthlyPension <= 0)
            throw new ArgumentException("Monthly pension must be greater than zero.");

        var annuity = new AnnuityPlan
        {
            MemberId         = request.MemberId,
            PlanType         = request.PlanType,
            PurchaseValue    = request.PurchaseValue,
            MonthlyPension   = request.MonthlyPension,
            AnnuityStartDate = request.AnnuityStartDate,
            NomineeDetails   = request.NomineeDetails,
            Status           = AnnuityStatus.Active
        };
        await _annuityRepo.AddAsync(annuity);
        await _unitOfWork.SaveChangesAsync();
        return await GetAnnuityAsync(annuity.AnnuityId);
    }

    public async Task<AnnuityResponse> UpdateAnnuityAsync(Guid annuityId, UpdateAnnuityRequest request)
    {
        var annuity = await _annuityRepo.FindByIdAsync(annuityId)
            ?? throw new KeyNotFoundException("Annuity plan not found.");

        annuity.PlanType       = request.PlanType;
        annuity.PurchaseValue  = request.PurchaseValue;
        annuity.MonthlyPension = request.MonthlyPension;
        annuity.NomineeDetails = request.NomineeDetails;
        if (request.Status.HasValue)
            annuity.Status = request.Status.Value;

        await _unitOfWork.SaveChangesAsync();
        return await GetAnnuityAsync(annuityId);
    }

    public async Task<AnnuityResponse> GetAnnuityAsync(Guid annuityId)
    {
        var a = await _annuityRepo.FindByIdAsync(annuityId)
            ?? throw new KeyNotFoundException("Annuity plan not found.");
        var member = await _memberRepo.FindByIdAsync(a.MemberId);
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;
        var isDisbursed = await _annuityRepo.ExistsDisbursementForMonthAsync(a.AnnuityId, currentMonth, currentYear);
        return new AnnuityResponse(a.AnnuityId, a.MemberId, member?.Name ?? "",
            a.PlanType, a.PurchaseValue, a.MonthlyPension,
            a.AnnuityStartDate, a.NomineeDetails, a.Status, isDisbursed);
    }

    public async Task<IEnumerable<AnnuityResponse>> GetAllAnnuitiesAsync()
    {
        var annuities = await _annuityRepo.GetAllAsync();
        var members = await _memberRepo.GetAllAsync();
        var memberDict = members.ToDictionary(m => m.MemberId, m => m.Name);
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        var list = new List<AnnuityResponse>();
        foreach (var a in annuities)
        {
            var isDisbursed = await _annuityRepo.ExistsDisbursementForMonthAsync(a.AnnuityId, currentMonth, currentYear);
            list.Add(new AnnuityResponse(
                a.AnnuityId, a.MemberId, memberDict.TryGetValue(a.MemberId, out var name) ? name : "",
                a.PlanType, a.PurchaseValue, a.MonthlyPension,
                a.AnnuityStartDate, a.NomineeDetails, a.Status, isDisbursed));
        }
        return list;
    }

    public async Task<IEnumerable<PensionDisbursementResponse>> GetDisbursementsAsync(Guid annuityId)
    {
        var disbursements = await _annuityRepo.GetDisbursementsAsync(annuityId);
        var members = await _memberRepo.GetAllAsync();
        var memberDict = members.ToDictionary(m => m.MemberId, m => m.Name);
        return disbursements.Select(d => new PensionDisbursementResponse(
            d.DisbursementId, d.AnnuityId, d.MemberId, memberDict.TryGetValue(d.MemberId, out var name) ? name : "",
            d.Month, d.Year, d.GrossAmount, d.TaxDeducted,
            d.NetAmount, d.DisbursedDate, d.Status));
    }

    public async Task<PensionDisbursementResponse> ProcessDisbursementAsync(ProcessDisbursementRequest request)
    {
        var annuity = await _annuityRepo.FindByIdAsync(request.AnnuityId)
            ?? throw new KeyNotFoundException("Annuity not found.");
        if (annuity.Status != AnnuityStatus.Active)
            throw new InvalidOperationException("Annuity is not active.");

        // ── Duplicate check: prevent paying the same month twice ──
        var alreadyDisbursed = await _annuityRepo.ExistsDisbursementForMonthAsync(
            request.AnnuityId, request.Month, request.Year);
        if (alreadyDisbursed)
            throw new InvalidOperationException(
                $"A disbursement for {request.Month}/{request.Year} has already been processed for this annuity plan.");

        var netAmount = annuity.MonthlyPension - request.TaxDeducted;
        var disbursement = new MonthlyPensionDisbursement
        {
            AnnuityId    = request.AnnuityId,
            MemberId     = annuity.MemberId,
            Month        = request.Month,
            Year         = request.Year,
            GrossAmount  = annuity.MonthlyPension,
            TaxDeducted  = request.TaxDeducted,
            NetAmount    = netAmount,
            DisbursedDate = DateTime.UtcNow,
            Status       = PensionDisbursementStatus.Disbursed
        };
        await _annuityRepo.AddDisbursementAsync(disbursement);

        // Deduct from PensionBalance ONLY — not the total EPF balance
        var account = await _accountRepo.FindActiveByMemberAsync(annuity.MemberId);
        if (account != null)
        {
            account.PensionBalance -= annuity.MonthlyPension;
            await _ledgerRepo.AddEntryAsync(new LedgerEntry
            {
                AccountId   = account.AccountId,
                EntryType   = EntryType.AnnuityDebit,
                Amount      = annuity.MonthlyPension,
                BalanceAfter = account.PensionBalance,
                ReferenceId = disbursement.DisbursementId.ToString(),
                Status      = LedgerEntryStatus.Posted
            });
        }

        await _unitOfWork.SaveChangesAsync();
        var d = await _annuityRepo.FindDisbursementByIdAsync(disbursement.DisbursementId);
        var member = d != null ? await _memberRepo.FindByIdAsync(d.MemberId) : null;
        return new PensionDisbursementResponse(
            d!.DisbursementId, d.AnnuityId, d.MemberId, member?.Name ?? "",
            d.Month, d.Year, d.GrossAmount, d.TaxDeducted,
            d.NetAmount, d.DisbursedDate, d.Status);
    }

    public async Task<AnnuityResponse> ProcessNomineeSettlementAsync(Guid annuityId, NomineeSettlementRequest request)
    {
        var annuity = await _annuityRepo.FindByIdAsync(annuityId)
            ?? throw new KeyNotFoundException("Annuity not found.");

        if (annuity.Status != AnnuityStatus.Active && annuity.Status != AnnuityStatus.Suspended)
            throw new InvalidOperationException("Annuity cannot be settled in its current state.");

        annuity.Status         = AnnuityStatus.Settled;
        annuity.NomineeDetails = $"{request.NomineeName} (Settled to {request.BankAccountRef})";

        // Deduct from pension balance only
        var account = await _accountRepo.FindActiveByMemberAsync(annuity.MemberId);
        if (account != null)
        {
            account.PensionBalance -= request.SettlementAmount;
            await _ledgerRepo.AddEntryAsync(new LedgerEntry
            {
                AccountId   = account.AccountId,
                EntryType   = EntryType.AnnuityDebit,
                Amount      = request.SettlementAmount,
                BalanceAfter = account.PensionBalance,
                ReferenceId = $"SETTLEMENT-{annuityId}",
                Status      = LedgerEntryStatus.Posted
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return await GetAnnuityAsync(annuityId);
    }

    public async Task<AnnuityResponse> TerminateAnnuityAsync(Guid annuityId)
    {
        var annuity = await _annuityRepo.FindByIdAsync(annuityId)
            ?? throw new KeyNotFoundException("Annuity not found.");
        annuity.Status = AnnuityStatus.Terminated;
        await _unitOfWork.SaveChangesAsync();
        return await GetAnnuityAsync(annuityId);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Private helpers
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Standard annuity formula: PMT = PV × r / (1 − (1+r)^−n)
    /// Uses 8% annual rate (close to EPS/EPF average).
    /// </summary>
    private static decimal CalcMonthlyPension(decimal balance, AnnuityPlanType planType, double annualRatePct = 8.0)
    {
        int payoutYears = planType switch
        {
            AnnuityPlanType.LifeAnnuity       => 20,
            AnnuityPlanType.JointAnnuity      => 30,
            AnnuityPlanType.TemporaryAnnuity  => 10,
            AnnuityPlanType.GuaranteedAnnuity => 15,
            _                                  => 20
        };
        int    n = payoutYears * 12;
        double r = annualRatePct / 100.0 / 12.0;
        if (r == 0) return balance / n;
        double factor = r / (1.0 - Math.Pow(1 + r, -n));
        return Math.Round((double)balance * factor, 2, MidpointRounding.AwayFromZero) is var res
            ? (decimal)res
            : 0;
    }



    private async Task<AnnuityRequestResponse> BuildRequestResponseAsync(AnnuityRequest r)
    {
        var member = await _memberRepo.FindByIdAsync(r.MemberId);
        return new AnnuityRequestResponse(
            r.RequestId,
            r.MemberId,
            member?.Name ?? "",
            member?.MembershipNumber ?? "",
            r.PlanType,
            r.PensionBalanceAtRequest,
            r.EstimatedMonthly,
            r.Note,
            r.Status,
            r.RequestedAt,
            r.ReviewedAt,
            r.ReviewNote);
    }
}
