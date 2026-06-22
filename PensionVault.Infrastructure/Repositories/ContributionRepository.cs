using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Repositories;

public class ContributionRepository : IContributionRepository
{
    private readonly AppDbContext _context;
    public ContributionRepository(AppDbContext context) => _context = context;

    public Task<ContributionRemittance?> FindRemittanceByIdAsync(Guid remittanceId)
        => _context.ContributionRemittances
            .Include(r => r.Employer)
            .FirstOrDefaultAsync(r => r.RemittanceId == remittanceId);

    public Task<List<ContributionRemittance>> GetAllRemittancesAsync()
        => _context.ContributionRemittances
            .Include(r => r.Employer)
            .OrderByDescending(r => r.RemittanceDate)
            .ToListAsync();

    public Task<List<ContributionRemittance>> GetByEmployerAsync(Guid employerId)
        => _context.ContributionRemittances
            .Include(r => r.Employer)
            .Where(r => r.EmployerId == employerId)
            .OrderByDescending(r => r.RemittanceDate)
            .ToListAsync();

    public Task<List<ContributionRemittance>> GetByStatusesAsync(params RemittanceStatus[] statuses)
        => _context.ContributionRemittances
            .Include(r => r.Employer)
            .Where(r => statuses.Contains(r.Status))
            .OrderByDescending(r => r.RemittanceDate)
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
            .Include(c => c.Member)
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.PostedDate)
            .ToListAsync();

    public async Task AddRemittanceAsync(ContributionRemittance remittance)
        => await _context.ContributionRemittances.AddAsync(remittance);

    public async Task AddContributionAsync(MemberContribution contribution)
        => await _context.MemberContributions.AddAsync(contribution);
}
