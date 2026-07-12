using PensionVault.Application.DTOs.Claims;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Domain.Constants;

namespace PensionVault.Application.Services;

public class ClaimService : IClaimService
{
    private readonly IClaimRepository _claimRepo;
    private readonly IMemberRepository _memberRepo;
    private readonly IFundAccountRepository _accountRepo;
    private readonly ILedgerRepository _ledgerRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;

    // Allowed reason categories for Partial Withdrawals to fix Bug #7
    private static readonly HashSet<string> AllowedPartialReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "Medical", "Housing", "Education", "Marriage"
    };

    public ClaimService(
        IClaimRepository claimRepo,
        IMemberRepository memberRepo,
        IFundAccountRepository accountRepo,
        ILedgerRepository ledgerRepo,
        INotificationRepository notificationRepo,
        IUserRepository userRepo,
        IUnitOfWork unitOfWork)
    {
        _claimRepo = claimRepo;
        _memberRepo = memberRepo;
        _accountRepo = accountRepo;
        _ledgerRepo = ledgerRepo;
        _notificationRepo = notificationRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClaimResponse> SubmitClaimAsync(CreateClaimRequest request)
    {
        // Guard Block: Fix Bug #2 & Bug #8 (Empty or missing Member ID)
        if (request.MemberId == Guid.Empty)
            throw new ArgumentException("A valid Member ID must be specified.");

        // Guard Block: Fix Bug #1, Bug #5 & Bug #8 (Negative values, typos, or zero amount inputs)
        if (request.EligibleAmount <= 0)
            throw new ArgumentException("The claim eligible amount must be strictly greater than zero.");

        // Guard Block: Fix Bug #7 (Prevent cross-wiring domain reason paths)
        if (request.ClaimType == ClaimType.PartialWithdrawal)
            throw new ArgumentException("Please use the dedicated partial-withdrawal route for early fund requests.");

        var member = await _memberRepo.FindByIdAsync(request.MemberId)
            ?? throw new KeyNotFoundException("Member not found.");

        var account = await _accountRepo.FindActiveByMemberAsync(request.MemberId);

        // Guard Block: Fix Bug #3 (Ensure requested total amount does not bypass active ledger balances)
        if (account == null || request.EligibleAmount > account.TotalBalance)
            throw new InvalidOperationException("Submission rejected: Requested claim amount exceeds available ledger balance.");

        var vestedAmount = Math.Round(account.TotalBalance * (account.VestingPercent / 100), 2);

        var claim = new BenefitClaim
        {
            MemberId = request.MemberId,
            ClaimType = request.ClaimType,
            ClaimDate = DateTime.UtcNow,
            EligibleAmount = request.EligibleAmount,
            VestedAmount = vestedAmount,
            TaxDeductible = Math.Round(request.EligibleAmount * 0.10m, 2),
            Status = ClaimStatus.Submitted
        };
        await _claimRepo.AddAsync(claim);

        await _notificationRepo.AddAsync(new Notification
        {
            UserId = member.UserId,
            Message = $"Your benefit claim of type {claim.ClaimType} for ₹{claim.EligibleAmount:N2} has been submitted successfully and is under review.",
            Category = NotificationCategory.Claim,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });

        var admins = await GetAdminUsersAsync();
        var adminNotifications = admins.Select(adminUser => new Notification
        {
            UserId = adminUser.UserId,
            Message = $"New claim submitted by {member.Name} of type {claim.ClaimType} for ₹{claim.EligibleAmount:N2}.",
            Category = NotificationCategory.Compliance,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });
        await _notificationRepo.AddRangeAsync(adminNotifications);

        await _unitOfWork.SaveChangesAsync();
        return await GetClaimAsync(claim.ClaimId);
    }

    public async Task<IEnumerable<ClaimResponse>> GetAllClaimsAsync()
    {
        var claims = await _claimRepo.GetAllAsync();
        var members = await _memberRepo.GetAllAsync();
        var memberDict = members.ToDictionary(m => m.MemberId, m => m.Name);
        var list = new List<ClaimResponse>();
        foreach (var c in claims)
        {
            var processedBy = c.ProcessedById.HasValue ? await _userRepo.FindByIdAsync(c.ProcessedById.Value) : null;
            list.Add(new ClaimResponse(
                c.ClaimId, c.MemberId, memberDict.TryGetValue(c.MemberId, out var name) ? name : "",
                c.ClaimType, c.ClaimDate, c.EligibleAmount,
                c.VestedAmount, c.TaxDeductible,
                processedBy?.Name, c.Status));
        }
        return list;
    }

    public async Task<ClaimResponse> GetClaimAsync(Guid claimId)
    {
        var claim = await _claimRepo.FindByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found.");
        var member = await _memberRepo.FindByIdAsync(claim.MemberId);
        var processedBy = claim.ProcessedById.HasValue ? await _userRepo.FindByIdAsync(claim.ProcessedById.Value) : null;
        return new ClaimResponse(
            claim.ClaimId, claim.MemberId, member?.Name ?? "",
            claim.ClaimType, claim.ClaimDate, claim.EligibleAmount,
            claim.VestedAmount, claim.TaxDeductible,
            processedBy?.Name, claim.Status);
    }

    public Task<ClaimResponse> ReviewClaimAsync(Guid claimId, Guid processedById)
        => UpdateStatusAsync(claimId, ClaimStatus.UnderReview, processedById);

    public Task<ClaimResponse> ApproveClaimAsync(Guid claimId, Guid processedById)
        => UpdateStatusAsync(claimId, ClaimStatus.Approved, processedById);

    public Task<ClaimResponse> RejectClaimAsync(Guid claimId, Guid processedById)
        => UpdateStatusAsync(claimId, ClaimStatus.Rejected, processedById);

    public async Task<DisbursementResponse> DisburseClaimAsync(Guid claimId, DisburseClaimRequest request)
    {
        // Guard Block: Fix Bug #6 (Prevent negative disbursement mathematical balance inflation hacks)
        if (request.DisbursedAmount <= 0)
            throw new ArgumentException("Disbursed payment value must be strictly greater than zero.");

        if (request.TaxDeducted < 0)
            throw new ArgumentException("Tax deduction configuration constraints cannot be negative.");

        var claim = await _claimRepo.FindByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found.");

        // Lifecycle Check: Prevent disbursing already finalized workflows
        if (claim.Status == ClaimStatus.Disbursed)
            throw new InvalidOperationException("This asset claim processing transaction has already been fully disbursed.");

        if (claim.Status != ClaimStatus.Approved)
            throw new InvalidOperationException("Claim must be approved before disbursement.");

        var disbursement = new ClaimDisbursement
        {
            ClaimId = claimId,
            MemberId = claim.MemberId,
            DisbursedAmount = request.DisbursedAmount,
            TaxDeducted = request.TaxDeducted,
            NetAmount = request.DisbursedAmount - request.TaxDeducted,
            BankAccountRef = request.BankAccountRef,
            DisbursedDate = DateTime.UtcNow,
            Status = DisbursementStatus.Processed
        };
        await _claimRepo.AddDisbursementAsync(disbursement);
        claim.Status = ClaimStatus.Disbursed;

        var account = await _accountRepo.FindActiveByMemberAsync(claim.MemberId);
        if (account != null)
        {
            account.TotalBalance -= disbursement.NetAmount;
            await _ledgerRepo.AddEntryAsync(new LedgerEntry
            {
                AccountId = account.AccountId,
                EntryType = EntryType.ClaimDebit,
                Amount = disbursement.NetAmount,
                BalanceAfter = account.TotalBalance,
                ReferenceId = claimId.ToString(),
                Status = LedgerEntryStatus.Posted
            });
        }

        var member = await _memberRepo.FindByIdAsync(claim.MemberId);
        if (member != null)
        {
            await _notificationRepo.AddAsync(new Notification
            {
                UserId = member.UserId,
                Message = $"Your claim payout of ₹{disbursement.NetAmount:N2} has been disbursed to bank account {disbursement.BankAccountRef}.",
                Category = NotificationCategory.Claim,
                Status = NotificationStatus.Unread,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return new DisbursementResponse(
            disbursement.DisbursementId, disbursement.ClaimId,
            disbursement.DisbursedAmount, disbursement.TaxDeducted,
            disbursement.NetAmount, disbursement.BankAccountRef,
            disbursement.DisbursedDate, disbursement.Status);
    }

    public async Task<ClaimResponse> SubmitPartialWithdrawalAsync(CreatePartialWithdrawalRequest request)
    {
        // Guard Block: Fix Bug #2 & Bug #8 (Empty or missing Member ID)
        if (request.MemberId == Guid.Empty)
            throw new ArgumentException("A valid Member ID identity constraint must be specified.");

        // Guard Block: Fix Bug #1, Bug #5 & Bug #8 (Negative values, typos, or zero amount inputs)
        if (request.RequestedAmount <= 0)
            throw new ArgumentException("The requested withdrawal amount must be strictly greater than zero.");

        // Guard Block: Fix Bug #7 (Block inappropriate reason mappings like 'Retirement')
        string normalizedReason = null;
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            var r = request.Reason.ToLowerInvariant();
            if (r.Contains("medical")) normalizedReason = "Medical";
            else if (r.Contains("housing") || r.Contains("house")) normalizedReason = "Housing";
            else if (r.Contains("education") || r.Contains("study") || r.Contains("college") || r.Contains("school")) normalizedReason = "Education";
            else if (r.Contains("marriage") || r.Contains("wedding")) normalizedReason = "Marriage";
        }

        if (normalizedReason == null)
        {
            throw new ArgumentException($"Invalid operational reason value. Allowed reason contexts are: {string.Join(", ", AllowedPartialReasons)}");
        }

        var member = await _memberRepo.FindByIdAsync(request.MemberId)
            ?? throw new KeyNotFoundException(ApplicationConstants.MemberNotFound);

        var account = await _accountRepo.FindActiveByMemberAsync(request.MemberId);

        // Guard Block: Fix Bug #3 (Strict ledger boundary validation for overdraft protection)
        if (account == null || request.RequestedAmount > account.TotalBalance)
            throw new InvalidOperationException("Submission rejected: Requested partial withdrawal amount exceeds available ledger balance.");

        var claim = new BenefitClaim
        {
            MemberId = request.MemberId,
            ClaimType = ClaimType.PartialWithdrawal,
            ClaimDate = DateTime.UtcNow,
            EligibleAmount = request.RequestedAmount,
            VestedAmount = request.RequestedAmount,
            TaxDeductible = 0,
            Status = ClaimStatus.Submitted
        };
        await _claimRepo.AddAsync(claim);

        await _notificationRepo.AddAsync(new Notification
        {
            UserId = member.UserId,
            Message = $"Your partial withdrawal claim for ₹{claim.EligibleAmount:N2} due to {request.Reason} has been submitted.",
            Category = NotificationCategory.Claim,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });

        var admins = await GetAdminUsersAsync();
        var adminNotifications = admins.Select(adminUser => new Notification
        {
            UserId = adminUser.UserId,
            Message = $"New partial withdrawal claim submitted by {member.Name} for ₹{claim.EligibleAmount:N2} due to {request.Reason}.",
            Category = NotificationCategory.Compliance,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });
        await _notificationRepo.AddRangeAsync(adminNotifications);

        await _unitOfWork.SaveChangesAsync();
        return await GetClaimAsync(claim.ClaimId);
    }

    public async Task<DisbursementResponse> DisbursePartialWithdrawalAsync(Guid claimId, DisbursePartialWithdrawalRequest request)
    {
        // Guard Block: Fix Bug #6 (Stop downstream zero/negative disbursement values immediately)
        if (request.DisbursedAmount <= 0)
            throw new ArgumentException("Disbursed payout amount context value must be strictly greater than zero.");

        var disburseRequest = new DisburseClaimRequest(request.DisbursedAmount, 0, request.BankAccountRef);
        return await DisburseClaimAsync(claimId, disburseRequest);
    }

    private async Task<ClaimResponse> UpdateStatusAsync(Guid claimId, ClaimStatus targetStatus, Guid processedById)
    {
        var claim = await _claimRepo.FindByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found.");

        // Guard Block: Fix Bug #4 (State verification check to break infinite loop updates)
        if (claim.Status == targetStatus)
            throw new InvalidOperationException($"State processing conflict: This claim transaction file is already in the '{targetStatus}' status registry context.");

        if (claim.Status == ClaimStatus.Disbursed)
            throw new InvalidOperationException("Modification error: State modifications are locked because funds are already fully disbursed.");

        claim.Status = targetStatus;
        claim.ProcessedById = processedById;

        var member = await _memberRepo.FindByIdAsync(claim.MemberId);
        if (member != null)
        {
            string message = targetStatus switch
            {
                ClaimStatus.UnderReview => $"Your claim of type {claim.ClaimType} is now under review.",
                ClaimStatus.Approved => $"Congratulations! Your claim of type {claim.ClaimType} for ₹{claim.EligibleAmount:N2} has been APPROVED.",
                ClaimStatus.Rejected => $"Your claim of type {claim.ClaimType} has been rejected.",
                _ => $"Your claim of type {claim.ClaimType} status has been updated to {targetStatus}."
            };
            await _notificationRepo.AddAsync(new Notification
            {
                UserId = member.UserId,
                Message = message,
                Category = NotificationCategory.Claim,
                Status = NotificationStatus.Unread,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return await GetClaimAsync(claimId);
    }

    private async Task<List<User>> GetAdminUsersAsync()
    {
        var admins = await _userRepo.GetByRoleAsync(UserRole.Admin);
        var fundAdmins = await _userRepo.GetByRoleAsync(UserRole.FundAdmin);
        var compliance = await _userRepo.GetByRoleAsync(UserRole.Compliance);
        
        var all = new List<User>();
        all.AddRange(admins);
        all.AddRange(fundAdmins);
        all.AddRange(compliance);
        return all.GroupBy(u => u.UserId).Select(g => g.First()).ToList();
    }
}
