using Contributions.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Contributions.Domain.Entities;
using Contributions.Data;

namespace Contributions.Data.Repositories;

public class ContributionRepository : IContributionRepository
{
    private readonly ContributionsDbContext _context;
    public ContributionRepository(ContributionsDbContext context) => _context = context;

    public Task<ContributionRemittance?> FindRemittanceByIdAsync(Guid remittanceId)
        => _context.ContributionRemittances
            .Include(r => r.MemberContributions)
            .FirstOrDefaultAsync(r => r.RemittanceId == remittanceId);

    public Task<List<ContributionRemittance>> GetAllRemittancesAsync()
        => _context.ContributionRemittances
            .Include(r => r.MemberContributions)
            .OrderByDescending(r => r.RemittanceDate)
            .ToListAsync();

    public Task<List<ContributionRemittance>> GetByEmployerAsync(Guid employerId)
        => _context.ContributionRemittances
            .Include(r => r.MemberContributions)
            .Where(r => r.EmployerId == employerId)
            .OrderByDescending(r => r.RemittanceDate)
            .ToListAsync();

    public Task<List<ContributionRemittance>> GetByStatusesAsync(params RemittanceStatus[] statuses)
        => _context.ContributionRemittances
            .Include(r => r.MemberContributions)
            .Where(r => statuses.Contains(r.Status))
            .ToListAsync();

    public Task<int> CountPostedContributionsAsync(Guid remittanceId)
        => _context.MemberContributions
            .CountAsync(c => c.RemittanceId == remittanceId && c.Status == ContributionStatus.Posted);

    public Task<decimal> SumReconciledAmountAsync(Guid remittanceId)
        => _context.MemberContributions
            .Where(c => c.RemittanceId == remittanceId && c.Status == ContributionStatus.Posted)
            .SumAsync(c => c.TotalAmount);

    public Task<List<MemberContribution>> GetByMemberAsync(Guid memberId)
        => _context.MemberContributions
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.PostedDate)
            .ToListAsync();

    public Task<MemberContribution?> FindContributionByIdAsync(Guid contributionId)
        => _context.MemberContributions.FirstOrDefaultAsync(c => c.ContributionId == contributionId);

    public async Task AddRemittanceAsync(ContributionRemittance remittance)
        => await _context.ContributionRemittances.AddAsync(remittance);

    public async Task AddContributionAsync(MemberContribution contribution)
        => await _context.MemberContributions.AddAsync(contribution);
}




