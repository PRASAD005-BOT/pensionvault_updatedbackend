using Microsoft.EntityFrameworkCore;
using PensionVault.Application.DTOs.Annuity;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;

namespace PensionVault.Application.Services;

public interface IAnnuityService
{
    Task<AnnuityResponse> CreateAnnuityAsync(CreateAnnuityRequest request);
    Task<AnnuityResponse> GetAnnuityAsync(Guid annuityId);
    Task<IEnumerable<PensionDisbursementResponse>> GetDisbursementsAsync(Guid annuityId);
    Task<PensionDisbursementResponse> ProcessDisbursementAsync(ProcessDisbursementRequest request);
    Task<NomineeSettlementResponse> ProcessNomineeSettlementAsync(NomineeSettlementRequest request);
    Task<AnnuityResponse> TerminateAnnuityAsync(Guid annuityId);
}

public class AnnuityService : IAnnuityService
{
    private readonly IAppDbContext _context;
    public AnnuityService(IAppDbContext context) => _context = context;

    public async Task<AnnuityResponse> CreateAnnuityAsync(CreateAnnuityRequest request)
    {
        var member = await _context.Members.FindAsync(request.MemberId)
            ?? throw new KeyNotFoundException("Member not found.");

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
        _context.AnnuityPlans.Add(annuity);
        await _context.SaveChangesAsync();
        return await GetAnnuityAsync(annuity.AnnuityId);
    }

    public async Task<AnnuityResponse> GetAnnuityAsync(Guid annuityId)
    {
        var a = await _context.AnnuityPlans
            .Include(x => x.Member)
            .FirstOrDefaultAsync(x => x.AnnuityId == annuityId)
            ?? throw new KeyNotFoundException("Annuity plan not found.");
        return new AnnuityResponse(a.AnnuityId, a.MemberId, a.Member.Name,
            a.PlanType, a.PurchaseValue, a.MonthlyPension,
            a.AnnuityStartDate, a.NomineeDetails, a.Status);
    }

    public async Task<IEnumerable<PensionDisbursementResponse>> GetDisbursementsAsync(Guid annuityId)
    {
        return await _context.MonthlyPensionDisbursements
            .Include(d => d.Member)
            .Where(d => d.AnnuityId == annuityId)
            .OrderByDescending(d => d.Year).ThenByDescending(d => d.Month)
            .Select(d => ToResponse(d))
            .ToListAsync();
    }

    public async Task<PensionDisbursementResponse> ProcessDisbursementAsync(ProcessDisbursementRequest request)
    {
        var annuity = await _context.AnnuityPlans.FindAsync(request.AnnuityId)
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
        _context.MonthlyPensionDisbursements.Add(disbursement);
        await _context.SaveChangesAsync();

        var d = await _context.MonthlyPensionDisbursements
            .Include(x => x.Member)
            .FirstAsync(x => x.DisbursementId == disbursement.DisbursementId);
        return ToResponse(d);
    }

    public async Task<NomineeSettlementResponse> ProcessNomineeSettlementAsync(NomineeSettlementRequest request)
    {
        var annuity = await _context.AnnuityPlans.FindAsync(request.AnnuityId)
            ?? throw new KeyNotFoundException("Annuity not found.");

        if (annuity.Status != AnnuityStatus.Active)
            throw new InvalidOperationException("Annuity is not active.");

        // Calculate remaining balance based on plan type
        var remainingBalance = CalculateRemainingBalance(annuity, request.DeathDate);

        // Create nominee disbursement record (using existing ClaimDisbursement entity)
        var claim = new BenefitClaim
        {
            MemberId = annuity.MemberId,
            ClaimType = ClaimType.DeathClaim,
            ClaimDate = request.DeathDate,
            EligibleAmount = remainingBalance,
            VestedAmount = remainingBalance,
            TaxDeductible = Math.Round(remainingBalance * 0.10m, 2),
            Status = ClaimStatus.Approved
        };
        _context.BenefitClaims.Add(claim);
        await _context.SaveChangesAsync();

        var disbursement = new ClaimDisbursement
        {
            ClaimId = claim.ClaimId,
            MemberId = annuity.MemberId,
            DisbursedAmount = remainingBalance,
            TaxDeducted = Math.Round(remainingBalance * 0.10m, 2),
            NetAmount = Math.Round(remainingBalance * 0.90m, 2),
            BankAccountRef = request.NomineeBankAccount,
            DisbursedDate = DateTime.UtcNow,
            Status = DisbursementStatus.Processed
        };
        _context.ClaimDisbursements.Add(disbursement);
        claim.Status = ClaimStatus.Disbursed;

        // Terminate annuity
        annuity.Status = AnnuityStatus.Settled;

        await _context.SaveChangesAsync();

        return new NomineeSettlementResponse(
            disbursement.DisbursementId,
            annuity.AnnuityId,
            annuity.MemberId,
            request.NomineeName,
            remainingBalance,
            disbursement.TaxDeducted,
            disbursement.NetAmount,
            request.DeathDate,
            DateTime.UtcNow
        );
    }

    public async Task<AnnuityResponse> TerminateAnnuityAsync(Guid annuityId)
    {
        var annuity = await _context.AnnuityPlans.FindAsync(annuityId)
            ?? throw new KeyNotFoundException("Annuity not found.");

        annuity.Status = AnnuityStatus.Settled;
        await _context.SaveChangesAsync();

        return await GetAnnuityAsync(annuityId);
    }

    private static PensionDisbursementResponse ToResponse(MonthlyPensionDisbursement d) => new(
        d.DisbursementId, d.AnnuityId, d.MemberId, d.Member?.Name ?? "",
        d.Month, d.Year, d.GrossAmount, d.TaxDeducted,
        d.NetAmount, d.DisbursedDate, d.Status);

    private static decimal CalculateRemainingBalance(AnnuityPlan annuity, DateTime deathDate)
    {
        // Calculate based on plan type and months remaining
        var monthsUsed = (deathDate.Year - annuity.AnnuityStartDate.Year) * 12 + 
                         (deathDate.Month - annuity.AnnuityStartDate.Month);
        
        if (monthsUsed < 0) monthsUsed = 0;

        decimal remainingBalance = 0;
        
        // Different plan types have different settlement rules
        if (annuity.PlanType == AnnuityPlanType.WithReturn)
        {
            // Return original purchase value minus disbursed amount
            var disbursedAmount = monthsUsed * annuity.MonthlyPension;
            remainingBalance = Math.Max(0, annuity.PurchaseValue - disbursedAmount);
        }
        else if (annuity.PlanType == AnnuityPlanType.LifeWithHeirs)
        {
            // Pay remaining purchase value to heirs
            remainingBalance = Math.Max(0, annuity.PurchaseValue / 2);
        }
        else
        {
            // Pure life annuity - no return
            remainingBalance = 0;
        }

        return remainingBalance;
    }
}
