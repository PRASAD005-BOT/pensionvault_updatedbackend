using Contributions.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Contributions.Domain.Entities;
using Contributions.Data;

namespace Contributions.Data.Repositories;

public class FundAccountRepository : IFundAccountRepository
{
    private readonly ContributionsDbContext _context;
    public FundAccountRepository(ContributionsDbContext context) => _context = context;

    public Task<FundAccount?> FindByIdAsync(Guid accountId)
        => _context.FundAccounts
            .Include(a => a.Scheme)
            .FirstOrDefaultAsync(a => a.AccountId == accountId);

    public Task<FundAccount?> FindActiveByMemberAsync(Guid memberId)
        => _context.FundAccounts
            .Include(a => a.Scheme)
            .FirstOrDefaultAsync(a => a.MemberId == memberId && a.Status == FundAccountStatus.Active);

    public Task<List<FundAccount>> GetByMemberAsync(Guid memberId)
        => _context.FundAccounts
            .Include(a => a.Scheme)
            .Where(a => a.MemberId == memberId)
            .ToListAsync();

    public Task<bool> ExistsByMemberAsync(Guid memberId)
        => _context.FundAccounts.AnyAsync(a => a.MemberId == memberId);

    public async Task AddAsync(FundAccount account)
    {
        // Detach the FundScheme if it is already tracked to avoid EF Core unique constraint violations
        if (account.Scheme != null)
        {
            var trackedScheme = _context.FundSchemes.Local.FirstOrDefault(s => s.SchemeId == account.Scheme.SchemeId);
            if (trackedScheme != null)
            {
                account.Scheme = null;
            }
            else
            {
                _context.Entry(account.Scheme).State = EntityState.Unchanged;
            }
        }
        await _context.FundAccounts.AddAsync(account);
    }
}




