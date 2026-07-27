using Claims.Services.DTOs;
using Claims.Domain.Entities;
using Claims.Domain.Repositories;
using Claims.Services.HttpClients;
using PensionVault.Shared.Contracts;
using PensionVault.Shared.HttpClients;

namespace Claims.Services;

public class ClaimService : IClaimService
{
    private readonly IClaimRepository _claimRepo;
    private readonly MembersServiceClient _memberClient;
    private readonly ContributionsServiceClient _contributionClient;
    private readonly NotificationServiceClient _notificationClient;
    private readonly AuditServiceClient _auditClient;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly HashSet<string> AllowedPartialReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "Medical", "Housing", "Education", "Marriage"
    };

    public ClaimService(
        IClaimRepository claimRepo,
        MembersServiceClient memberClient,
        ContributionsServiceClient contributionClient,
        NotificationServiceClient notificationClient,
        AuditServiceClient auditClient,
        IUnitOfWork unitOfWork)
    {
        _claimRepo = claimRepo;
        _memberClient = memberClient;
        _contributionClient = contributionClient;
        _notificationClient = notificationClient;
        _auditClient = auditClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClaimResponse> SubmitClaimAsync(CreateClaimRequest request)
    {
        if (request.MemberId == Guid.Empty)
            throw new ArgumentException("A valid Member ID must be specified.");

        if (request.EligibleAmount <= 0)
            throw new ArgumentException("The claim eligible amount must be strictly greater than zero.");

        if (request.ClaimType == ClaimType.PartialWithdrawal)
            throw new ArgumentException("Please use the dedicated partial-withdrawal route for early fund requests.");

        var member = await _memberClient.GetMemberByIdAsync(request.MemberId)
            ?? throw new KeyNotFoundException("Member not found.");

        var account = await _contributionClient.GetActiveByMemberAsync(request.MemberId);

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
        await _unitOfWork.SaveChangesAsync();

        // Send notifications
        var notifications = new List<CreateNotificationRequest>
        {
            new(member.UserId, $"Your benefit claim of type {claim.ClaimType} for ₹{claim.EligibleAmount:N2} has been submitted successfully and is under review.", "Claim")
        };

        var admins = await GetAdminUsersAsync();
        notifications.AddRange(admins.Select(admin =>
            new CreateNotificationRequest(admin.UserId, $"New claim submitted by {member.Name} of type {claim.ClaimType} for ₹{claim.EligibleAmount:N2}.", "Compliance")));

        await _notificationClient.SendBulkNotificationsAsync(notifications);

        return await GetClaimAsync(claim.ClaimId);
    }

    public async Task<IEnumerable<ClaimResponse>> GetAllClaimsAsync()
    {
        var claims = await _claimRepo.GetAllAsync();
        var list = new List<ClaimResponse>();
        foreach (var c in claims)
        {
            var member = await _memberClient.GetMemberByIdAsync(c.MemberId);
            var processedBy = c.ProcessedById.HasValue ? await _memberClient.GetMemberByUserIdAsync(c.ProcessedById.Value) : null;
            list.Add(new ClaimResponse(
                c.ClaimId, c.MemberId, member?.Name ?? "",
                c.ClaimType.ToString(), c.ClaimDate, c.EligibleAmount,
                c.VestedAmount, c.TaxDeductible,
                processedBy?.Name, c.Status.ToString()));
        }
        return list;
    }

    public async Task<ClaimResponse> GetClaimAsync(Guid claimId)
    {
        var claim = await _claimRepo.FindByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found.");
        var member = await _memberClient.GetMemberByIdAsync(claim.MemberId);
        var processedBy = claim.ProcessedById.HasValue ? await _memberClient.GetMemberByUserIdAsync(claim.ProcessedById.Value) : null;
        return new ClaimResponse(
            claim.ClaimId, claim.MemberId, member?.Name ?? "",
            claim.ClaimType.ToString(), claim.ClaimDate, claim.EligibleAmount,
            claim.VestedAmount, claim.TaxDeductible,
            processedBy?.Name, claim.Status.ToString());
    }

    public Task<ClaimResponse> ReviewClaimAsync(Guid claimId, Guid processedById)
        => UpdateStatusAsync(claimId, ClaimStatus.UnderReview, processedById);

    public Task<ClaimResponse> ApproveClaimAsync(Guid claimId, Guid processedById)
        => UpdateStatusAsync(claimId, ClaimStatus.Approved, processedById);

    public Task<ClaimResponse> RejectClaimAsync(Guid claimId, Guid processedById)
        => UpdateStatusAsync(claimId, ClaimStatus.Rejected, processedById);

    public async Task<DisbursementResponse> DisburseClaimAsync(Guid claimId, DisburseClaimRequest request)
    {
        if (request.DisbursedAmount <= 0)
            throw new ArgumentException("Disbursed payment value must be strictly greater than zero.");

        if (request.TaxDeducted < 0)
            throw new ArgumentException("Tax deduction configuration constraints cannot be negative.");

        var claim = await _claimRepo.FindByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found.");

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

        // Post ledger entry to Contributions Service
        var account = await _contributionClient.GetActiveByMemberAsync(claim.MemberId);
        if (account != null)
        {
            await _contributionClient.AddLedgerEntryAsync(account.AccountId, "ClaimDebit", disbursement.NetAmount, claimId.ToString());
        }

        await _unitOfWork.SaveChangesAsync();

        // Send notifications
        var member = await _memberClient.GetMemberByIdAsync(claim.MemberId);
        if (member != null)
        {
            await _notificationClient.SendBulkNotificationsAsync(new List<CreateNotificationRequest>
            {
                new(member.UserId, $"Your claim payout of ₹{disbursement.NetAmount:N2} has been disbursed to bank account {disbursement.BankAccountRef}.", "Claim")
            });
        }

        return new DisbursementResponse(
            disbursement.DisbursementId, disbursement.ClaimId,
            disbursement.DisbursedAmount, disbursement.TaxDeducted,
            disbursement.NetAmount, disbursement.BankAccountRef,
            disbursement.DisbursedDate, disbursement.Status.ToString());
    }

    public async Task<ClaimResponse> SubmitPartialWithdrawalAsync(CreatePartialWithdrawalRequest request)
    {
        if (request.MemberId == Guid.Empty)
            throw new ArgumentException("A valid Member ID identity constraint must be specified.");

        if (request.RequestedAmount <= 0)
            throw new ArgumentException("The requested withdrawal amount must be strictly greater than zero.");

        string? normalizedReason = null;
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

        var member = await _memberClient.GetMemberByIdAsync(request.MemberId)
            ?? throw new KeyNotFoundException("Member not found.");

        var account = await _contributionClient.GetActiveByMemberAsync(request.MemberId);

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
        await _unitOfWork.SaveChangesAsync();

        // Send notifications
        var notifications = new List<CreateNotificationRequest>
        {
            new(member.UserId, $"Your partial withdrawal claim for ₹{claim.EligibleAmount:N2} due to {request.Reason} has been submitted.", "Claim")
        };

        var admins = await GetAdminUsersAsync();
        notifications.AddRange(admins.Select(admin =>
            new CreateNotificationRequest(admin.UserId, $"New partial withdrawal claim submitted by {member.Name} for ₹{claim.EligibleAmount:N2} due to {request.Reason}.", "Compliance")));

        await _notificationClient.SendBulkNotificationsAsync(notifications);

        return await GetClaimAsync(claim.ClaimId);
    }

    public async Task<DisbursementResponse> DisbursePartialWithdrawalAsync(Guid claimId, DisbursePartialWithdrawalRequest request)
    {
        if (request.DisbursedAmount <= 0)
            throw new ArgumentException("Disbursed payout amount context value must be strictly greater than zero.");

        var disburseRequest = new DisburseClaimRequest(request.DisbursedAmount, 0, request.BankAccountRef);
        return await DisburseClaimAsync(claimId, disburseRequest);
    }

    private async Task<ClaimResponse> UpdateStatusAsync(Guid claimId, ClaimStatus targetStatus, Guid processedById)
    {
        var claim = await _claimRepo.FindByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found.");

        if (claim.Status == targetStatus)
            throw new InvalidOperationException($"State processing conflict: This claim transaction file is already in the '{targetStatus}' status registry context.");

        if (claim.Status == ClaimStatus.Disbursed)
            throw new InvalidOperationException("Modification error: State modifications are locked because funds are already fully disbursed.");

        claim.Status = targetStatus;
        claim.ProcessedById = processedById;
        await _unitOfWork.SaveChangesAsync();

        // Trigger HTTP audit log to Members Service
        await _auditClient.WriteAsync(processedById, $"{targetStatus}Claim", "Claim", claimId.ToString());

        // Send notification
        var member = await _memberClient.GetMemberByIdAsync(claim.MemberId);
        if (member != null)
        {
            string message = targetStatus switch
            {
                ClaimStatus.UnderReview => $"Your claim of type {claim.ClaimType} is now under review.",
                ClaimStatus.Approved => $"Congratulations! Your claim of type {claim.ClaimType} for ₹{claim.EligibleAmount:N2} has been APPROVED.",
                ClaimStatus.Rejected => $"Your claim of type {claim.ClaimType} has been rejected.",
                _ => $"Your claim of type {claim.ClaimType} status has been updated to {targetStatus}."
            };
            await _notificationClient.SendBulkNotificationsAsync(new List<CreateNotificationRequest>
            {
                new(member.UserId, message, "Claim")
            });
        }

        return await GetClaimAsync(claimId);
    }

    private async Task<List<UserSummaryResponse>> GetAdminUsersAsync()
    {
        var admins = await _memberClient.GetUsersByRoleAsync("Admin");
        var fundAdmins = await _memberClient.GetUsersByRoleAsync("FundAdmin");
        var compliance = await _memberClient.GetUsersByRoleAsync("Compliance");
        
        var all = new List<UserSummaryResponse>();
        all.AddRange(admins);
        all.AddRange(fundAdmins);
        all.AddRange(compliance);
        return all.GroupBy(u => u.UserId).Select(g => g.First()).ToList();
    }
}





