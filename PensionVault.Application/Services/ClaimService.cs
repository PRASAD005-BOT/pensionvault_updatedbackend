using PensionVault.Application.DTOs.Claims;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Application.Services;

public class ClaimService : IClaimService
{
    private readonly IClaimRepository _claimRepo;
    private readonly IMemberRepository _memberRepo;
    private readonly IFundAccountRepository _accountRepo;
    private readonly ILedgerRepository _ledgerRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ClaimService(
        IClaimRepository claimRepo,
        IMemberRepository memberRepo,
        IFundAccountRepository accountRepo,
        ILedgerRepository ledgerRepo,
        INotificationRepository notificationRepo,
        IUnitOfWork unitOfWork)
    {
        _claimRepo = claimRepo;
        _memberRepo = memberRepo;
        _accountRepo = accountRepo;
        _ledgerRepo = ledgerRepo;
        _notificationRepo = notificationRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClaimResponse> SubmitClaimAsync(CreateClaimRequest request)
    {
        var member = await _memberRepo.FindByIdAsync(request.MemberId)
            ?? throw new KeyNotFoundException("Member not found.");

        var account = await _accountRepo.FindActiveByMemberAsync(request.MemberId);
        var vestedAmount = account != null
            ? Math.Round(account.TotalBalance * (account.VestingPercent / 100), 2)
            : 0;

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

        await _unitOfWork.SaveChangesAsync();
        return await GetClaimAsync(claim.ClaimId);
    }

    public async Task<IEnumerable<ClaimResponse>> GetAllClaimsAsync()
    {
        var claims = await _claimRepo.GetAllAsync();
        return claims.Select(ToResponse);
    }

    public async Task<ClaimResponse> GetClaimAsync(Guid claimId)
    {
        var claim = await _claimRepo.FindByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found.");
        return ToResponse(claim);
    }

    public Task<ClaimResponse> ReviewClaimAsync(Guid claimId, Guid processedById)
        => UpdateStatusAsync(claimId, ClaimStatus.UnderReview, processedById);

    public Task<ClaimResponse> ApproveClaimAsync(Guid claimId, Guid processedById)
        => UpdateStatusAsync(claimId, ClaimStatus.Approved, processedById);

    public Task<ClaimResponse> RejectClaimAsync(Guid claimId, Guid processedById)
        => UpdateStatusAsync(claimId, ClaimStatus.Rejected, processedById);

    public async Task<DisbursementResponse> DisburseClaimAsync(Guid claimId, DisburseClaimRequest request)
    {
        var claim = await _claimRepo.FindByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found.");
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

        if (claim.Member != null)
        {
            await _notificationRepo.AddAsync(new Notification
            {
                UserId = claim.Member.UserId,
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
        var member = await _memberRepo.FindByIdAsync(request.MemberId)
            ?? throw new KeyNotFoundException("Member not found.");

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

        await _unitOfWork.SaveChangesAsync();
        return await GetClaimAsync(claim.ClaimId);
    }

    public async Task<DisbursementResponse> DisbursePartialWithdrawalAsync(Guid claimId, DisbursePartialWithdrawalRequest request)
    {
        var disburseRequest = new DisburseClaimRequest(request.DisbursedAmount, 0, request.BankAccountRef);
        return await DisburseClaimAsync(claimId, disburseRequest);
    }

    private async Task<ClaimResponse> UpdateStatusAsync(Guid claimId, ClaimStatus status, Guid processedById)
    {
        var claim = await _claimRepo.FindByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found.");
        claim.Status = status;
        claim.ProcessedById = processedById;

        if (claim.Member != null)
        {
            string message = status switch
            {
                ClaimStatus.UnderReview => $"Your claim of type {claim.ClaimType} is now under review.",
                ClaimStatus.Approved => $"Congratulations! Your claim of type {claim.ClaimType} for ₹{claim.EligibleAmount:N2} has been APPROVED.",
                ClaimStatus.Rejected => $"Your claim of type {claim.ClaimType} has been rejected.",
                _ => $"Your claim of type {claim.ClaimType} status has been updated to {status}."
            };
            await _notificationRepo.AddAsync(new Notification
            {
                UserId = claim.Member.UserId,
                Message = message,
                Category = NotificationCategory.Claim,
                Status = NotificationStatus.Unread,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return await GetClaimAsync(claimId);
    }

    private static ClaimResponse ToResponse(BenefitClaim c) => new(
        c.ClaimId, c.MemberId, c.Member?.Name ?? "",
        c.ClaimType, c.ClaimDate, c.EligibleAmount,
        c.VestedAmount, c.TaxDeductible,
        c.ProcessedBy?.Name, c.Status);
}
