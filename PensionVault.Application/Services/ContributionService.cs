using Microsoft.EntityFrameworkCore;
using PensionVault.Application.DTOs.Contributions;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;

namespace PensionVault.Application.Services;

public interface IContributionService
{
    Task<RemittanceResponse> CreateRemittanceAsync(CreateRemittanceRequest request);
    Task<RemittanceResponse> GetRemittanceAsync(Guid remittanceId);
    Task<IEnumerable<RemittanceResponse>> GetEmployerRemittancesAsync(Guid employerId);
    Task<RemittanceResponse> ReconcileAsync(Guid remittanceId);
    Task<ReconciliationReportResponse> GetReconciliationReportAsync(Guid remittanceId);
    Task<IEnumerable<RemittanceResponse>> GetDefaulterRemittancesAsync();
    Task<IEnumerable<OverdueRemittanceResponse>> GetOverdueRemittancesAsync(int delayDaysThreshold = 30);
    Task<DefaulterSummaryResponse> GetDefaulterSummaryAsync(Guid employerId);
    Task<IEnumerable<MemberContributionResponse>> GetMemberContributionsAsync(Guid memberId);
}

public class ContributionService : IContributionService
{
    private readonly IAppDbContext _context;
    public ContributionService(IAppDbContext context) => _context = context;

    public async Task<RemittanceResponse> CreateRemittanceAsync(CreateRemittanceRequest request)
    {
        var employer = await _context.Employers.FindAsync(request.EmployerId)
            ?? throw new KeyNotFoundException("Employer not found.");

        var total = request.TotalEmployeeShare + request.TotalEmployerShare;
        var remittance = new ContributionRemittance
        {
            EmployerId = request.EmployerId,
            RemittancePeriod = request.RemittancePeriod,
            TotalEmployeeShare = request.TotalEmployeeShare,
            TotalEmployerShare = request.TotalEmployerShare,
            TotalAmount = total,
            RemittanceDate = DateTime.UtcNow,
            CoverageCount = request.CoverageCount,
            Status = RemittanceStatus.Received
        };
        _context.ContributionRemittances.Add(remittance);

        foreach (var item in request.MemberContributions)
        {
            var contribution = new MemberContribution
            {
                RemittanceId = remittance.RemittanceId,
                MemberId = item.MemberId,
                Period = request.RemittancePeriod,
                EmployeeAmount = item.EmployeeAmount,
                EmployerAmount = item.EmployerAmount,
                TotalAmount = item.EmployeeAmount + item.EmployerAmount,
                PostedDate = DateTime.UtcNow,
                Status = ContributionStatus.Posted
            };
            _context.MemberContributions.Add(contribution);

            // Post to ledger
            var account = await _context.FundAccounts
                .FirstOrDefaultAsync(a => a.MemberId == item.MemberId && a.Status == FundAccountStatus.Active);
            if (account != null)
            {
                account.EmployeeContributionBalance += item.EmployeeAmount;
                account.EmployerContributionBalance += item.EmployerAmount;
                account.TotalBalance += contribution.TotalAmount;

                _context.LedgerEntries.Add(new LedgerEntry
                {
                    AccountId = account.AccountId,
                    EntryType = EntryType.ContributionCredit,
                    Amount = contribution.TotalAmount,
                    BalanceAfter = account.TotalBalance,
                    ReferenceId = remittance.RemittanceId.ToString(),
                    Status = LedgerEntryStatus.Posted
                });
            }
        }

        await _context.SaveChangesAsync();
        return await GetRemittanceAsync(remittance.RemittanceId);
    }

    public async Task<RemittanceResponse> GetRemittanceAsync(Guid remittanceId)
    {
        var r = await _context.ContributionRemittances
            .Include(r => r.Employer)
            .FirstOrDefaultAsync(r => r.RemittanceId == remittanceId)
            ?? throw new KeyNotFoundException("Remittance not found.");
        return ToResponse(r);
    }

    public async Task<IEnumerable<RemittanceResponse>> GetEmployerRemittancesAsync(Guid employerId)
    {
        return await _context.ContributionRemittances
            .Include(r => r.Employer)
            .Where(r => r.EmployerId == employerId)
            .OrderByDescending(r => r.RemittanceDate)
            .Select(r => ToResponse(r))
            .ToListAsync();
    }

    public async Task<RemittanceResponse> ReconcileAsync(Guid remittanceId)
    {
        var remittance = await _context.ContributionRemittances.FindAsync(remittanceId)
            ?? throw new KeyNotFoundException("Remittance not found.");

        var postedCount = await _context.MemberContributions
            .CountAsync(c => c.RemittanceId == remittanceId && c.Status == ContributionStatus.Posted);

        var shortfallCount = remittance.CoverageCount - postedCount;
        
        // Determine status based on coverage
        if (postedCount == remittance.CoverageCount)
        {
            remittance.Status = RemittanceStatus.Reconciled;
        }
        else if (shortfallCount > 0 && shortfallCount <= remittance.CoverageCount / 10)
        {
            // Minor shortfall (less than 10%)
            remittance.Status = RemittanceStatus.Shortfall;
        }
        else if (shortfallCount > remittance.CoverageCount / 2)
        {
            // Significant shortfall (more than 50%) - mark as potential default
            remittance.Status = RemittanceStatus.Default;
            
            // Mark employer status
            var employer = await _context.Employers.FindAsync(remittance.EmployerId);
            if (employer != null)
            {
                employer.Status = EmployerStatus.Defaulter;
            }
        }
        else
        {
            remittance.Status = RemittanceStatus.Shortfall;
        }

        await _context.SaveChangesAsync();
        return await GetRemittanceAsync(remittanceId);
    }

    public async Task<ReconciliationReportResponse> GetReconciliationReportAsync(Guid remittanceId)
    {
        var remittance = await _context.ContributionRemittances
            .Include(r => r.Employer)
            .FirstOrDefaultAsync(r => r.RemittanceId == remittanceId)
            ?? throw new KeyNotFoundException("Remittance not found.");

        var contributions = await _context.MemberContributions
            .Include(c => c.Member)
            .Where(c => c.RemittanceId == remittanceId)
            .ToListAsync();

        var postedContributions = contributions.Where(c => c.Status == ContributionStatus.Posted).ToList();
        var shortfallCount = remittance.CoverageCount - postedContributions.Count;

        return new ReconciliationReportResponse(
            remittanceId,
            remittance.EmployerId,
            remittance.Employer?.CompanyName ?? "",
            remittance.RemittancePeriod,
            remittance.CoverageCount,
            postedContributions.Count,
            shortfallCount,
            remittance.TotalEmployeeShare,
            remittance.TotalEmployerShare,
            remittance.TotalAmount,
            postedContributions.Sum(c => c.TotalAmount),
            remittance.TotalAmount - postedContributions.Sum(c => c.TotalAmount),
            remittance.Status,
            remittance.RemittanceDate
        );
    }

    public async Task<IEnumerable<RemittanceResponse>> GetDefaulterRemittancesAsync()
    {
        return await _context.ContributionRemittances
            .Include(r => r.Employer)
            .Where(r => r.Status == RemittanceStatus.Default || r.Employer!.Status == EmployerStatus.Defaulter)
            .OrderByDescending(r => r.RemittanceDate)
            .Select(r => ToResponse(r))
            .ToListAsync();
    }

    public async Task<IEnumerable<OverdueRemittanceResponse>> GetOverdueRemittancesAsync(int delayDaysThreshold = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-delayDaysThreshold);
        
        return await _context.ContributionRemittances
            .Include(r => r.Employer)
            .Where(r => r.RemittanceDate <= cutoffDate && 
                       (r.Status == RemittanceStatus.Received || 
                        r.Status == RemittanceStatus.Shortfall || 
                        r.Status == RemittanceStatus.Default))
            .OrderBy(r => r.RemittanceDate)
            .Select(r => new OverdueRemittanceResponse(
                r.RemittanceId,
                r.EmployerId,
                r.Employer!.CompanyName,
                r.RemittancePeriod,
                r.TotalAmount,
                r.RemittanceDate,
                DateTime.UtcNow.Subtract(r.RemittanceDate).Days,
                r.Status
            ))
            .ToListAsync();
    }

    public async Task<DefaulterSummaryResponse> GetDefaulterSummaryAsync(Guid employerId)
    {
        var employer = await _context.Employers.FindAsync(employerId)
            ?? throw new KeyNotFoundException("Employer not found.");

        var allRemittances = await _context.ContributionRemittances
            .Where(r => r.EmployerId == employerId)
            .ToListAsync();

        var defaultRemittances = allRemittances.Where(r => r.Status == RemittanceStatus.Default).ToList();
        var shortfallRemittances = allRemittances.Where(r => r.Status == RemittanceStatus.Shortfall).ToList();

        var totalDefaultAmount = defaultRemittances.Sum(r => r.TotalAmount);
        var totalShortfallAmount = shortfallRemittances.Sum(r => r.TotalAmount);

        return new DefaulterSummaryResponse(
            employerId,
            employer.CompanyName,
            employer.Status,
            allRemittances.Count,
            defaultRemittances.Count,
            shortfallRemittances.Count,
            allRemittances.Where(r => r.Status == RemittanceStatus.Reconciled).Count(),
            totalDefaultAmount,
            totalShortfallAmount,
            defaultRemittances.OrderByDescending(r => r.RemittanceDate).FirstOrDefault()?.RemittanceDate ?? DateTime.MinValue
        );
    }

    public async Task<IEnumerable<MemberContributionResponse>> GetMemberContributionsAsync(Guid memberId)
    {
        return await _context.MemberContributions
            .Include(c => c.Member)
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.PostedDate)
            .Select(c => new MemberContributionResponse(
                c.ContributionId, c.MemberId, c.Member.Name,
                c.Period, c.EmployeeAmount, c.EmployerAmount,
                c.TotalAmount, c.PostedDate, c.Status))
            .ToListAsync();
    }

    private static RemittanceResponse ToResponse(ContributionRemittance r) => new(
        r.RemittanceId, r.EmployerId, r.Employer?.CompanyName ?? "",
        r.RemittancePeriod, r.TotalEmployeeShare, r.TotalEmployerShare,
        r.TotalAmount, r.RemittanceDate, r.CoverageCount, r.Status);
}
