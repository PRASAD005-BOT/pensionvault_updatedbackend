using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Repositories;

public class FundAccountRepository : IFundAccountRepository
{
    private readonly AppDbContext _context;
    public FundAccountRepository(AppDbContext context) => _context = context;

    public Task<FundAccount?> FindByIdAsync(Guid accountId)
        => _context.FundAccounts.FindAsync(accountId).AsTask();

    public Task<FundAccount?> FindActiveByMemberAsync(Guid memberId)
        => _context.FundAccounts
            .FirstOrDefaultAsync(a => a.MemberId == memberId && a.Status == FundAccountStatus.Active);

    public Task<List<FundAccount>> GetByMemberAsync(Guid memberId)
        => _context.FundAccounts
            .Include(a => a.Scheme)
            .Where(a => a.MemberId == memberId)
            .ToListAsync();

    public Task<bool> ExistsByMemberAsync(Guid memberId)
        => _context.FundAccounts.AnyAsync(a => a.MemberId == memberId);

    public async Task AddAsync(FundAccount account)
        => await _context.FundAccounts.AddAsync(account);
}
