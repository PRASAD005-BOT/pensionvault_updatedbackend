using PensionVault.Application.DTOs.Annuity;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Application.Services;

public class AnnuityService : IAnnuityService
{
    private readonly IAnnuityRepository _annuityRepo;
    private readonly IFundAccountRepository _accountRepo;
    private readonly ILedgerRepository _ledgerRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AnnuityService(
        IAnnuityRepository annuityRepo,
        IFundAccountRepository accountRepo,
        ILedgerRepository ledgerRepo,
        IUnitOfWork unitOfWork)
    {
        _annuityRepo = annuityRepo;
        _accountRepo = accountRepo;
        _ledgerRepo = ledgerRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<AnnuityResponse> CreateAnnuityAsync(CreateAnnuityRequest request)
    {
        var annuity = new AnnuityPlan
        {
            MemberId = request.MemberId,
            PlanType = request.PlanType,
            PurchaseValue = request.PurchaseValue,
            MonthlyPension = request.MonthlyPension,
            AnnuityStartDate = request.AnnuityStartDate,
            NomineeDetails = request.NomineeDetails,
            Status = AnnuityStatus.Active
        };
        await _annuityRepo.AddAsync(annuity);
        await _unitOfWork.SaveChangesAsync();
        return await GetAnnuityAsync(annuity.AnnuityId);
    }

    public async Task<AnnuityResponse> GetAnnuityAsync(Guid annuityId)
    {
        var a = await _annuityRepo.FindByIdAsync(annuityId)
            ?? throw new KeyNotFoundException("Annuity plan not found.");
        return new AnnuityResponse(a.AnnuityId, a.MemberId, a.Member?.Name ?? "",
            a.PlanType, a.PurchaseValue, a.MonthlyPension,
            a.AnnuityStartDate, a.NomineeDetails, a.Status);
    }

    public async Task<IEnumerable<AnnuityResponse>> GetAllAnnuitiesAsync()
    {
        var annuities = await _annuityRepo.GetAllAsync();
        return annuities.Select(a => new AnnuityResponse(
            a.AnnuityId, a.MemberId, a.Member?.Name ?? "",
            a.PlanType, a.PurchaseValue, a.MonthlyPension,
            a.AnnuityStartDate, a.NomineeDetails, a.Status));
    }

    public async Task<IEnumerable<PensionDisbursementResponse>> GetDisbursementsAsync(Guid annuityId)
    {
        var disbursements = await _annuityRepo.GetDisbursementsAsync(annuityId);
        return disbursements.Select(ToResponse);
    }

    public async Task<PensionDisbursementResponse> ProcessDisbursementAsync(ProcessDisbursementRequest request)
    {
        var annuity = await _annuityRepo.FindByIdAsync(request.AnnuityId)
            ?? throw new KeyNotFoundException("Annuity not found.");
        if (annuity.Status != AnnuityStatus.Active)
            throw new InvalidOperationException("Annuity is not active.");

        var netAmount = annuity.MonthlyPension - request.TaxDeducted;
        var disbursement = new MonthlyPensionDisbursement
        {
            AnnuityId = request.AnnuityId,
            MemberId = annuity.MemberId,
            Month = request.Month,
            Year = request.Year,
            GrossAmount = annuity.MonthlyPension,
            TaxDeducted = request.TaxDeducted,
            NetAmount = netAmount,
            DisbursedDate = DateTime.UtcNow,
            Status = PensionDisbursementStatus.Disbursed
        };
        await _annuityRepo.AddDisbursementAsync(disbursement);

        var account = await _accountRepo.FindActiveByMemberAsync(annuity.MemberId);
        if (account != null)
        {
            account.TotalBalance -= annuity.MonthlyPension;
            await _ledgerRepo.AddEntryAsync(new LedgerEntry
            {
                AccountId = account.AccountId,
                EntryType = EntryType.AnnuityDebit,
                Amount = annuity.MonthlyPension,
                BalanceAfter = account.TotalBalance,
                ReferenceId = disbursement.DisbursementId.ToString(),
                Status = LedgerEntryStatus.Posted
            });
        }

        await _unitOfWork.SaveChangesAsync();
        var d = await _annuityRepo.FindDisbursementByIdAsync(disbursement.DisbursementId);
        return ToResponse(d!);
    }

    public async Task<AnnuityResponse> ProcessNomineeSettlementAsync(Guid annuityId, NomineeSettlementRequest request)
    {
        var annuity = await _annuityRepo.FindByIdAsync(annuityId)
            ?? throw new KeyNotFoundException("Annuity not found.");

        if (annuity.Status != AnnuityStatus.Active && annuity.Status != AnnuityStatus.Suspended)
            throw new InvalidOperationException("Annuity cannot be settled in its current state.");

        annuity.Status = AnnuityStatus.Settled;
        annuity.NomineeDetails = $"{request.NomineeName} (Settled to {request.BankAccountRef})";

        var account = await _accountRepo.FindActiveByMemberAsync(annuity.MemberId);
        if (account != null)
        {
            account.TotalBalance -= request.SettlementAmount;
            await _ledgerRepo.AddEntryAsync(new LedgerEntry
            {
                AccountId = account.AccountId,
                EntryType = EntryType.AnnuityDebit,
                Amount = request.SettlementAmount,
                BalanceAfter = account.TotalBalance,
                ReferenceId = $"SETTLEMENT-{annuityId}",
                Status = LedgerEntryStatus.Posted
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

    private static PensionDisbursementResponse ToResponse(MonthlyPensionDisbursement d) => new(
        d.DisbursementId, d.AnnuityId, d.MemberId, d.Member?.Name ?? "",
        d.Month, d.Year, d.GrossAmount, d.TaxDeducted,
        d.NetAmount, d.DisbursedDate, d.Status);
}
