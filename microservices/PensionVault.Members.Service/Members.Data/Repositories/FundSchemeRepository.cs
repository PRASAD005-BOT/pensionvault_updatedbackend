using Members.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Members.Domain.Entities;
using Members.Data;

namespace Members.Data.Repositories;

public class FundSchemeRepository : IFundSchemeRepository
{
    private readonly MembersDbContext _context;
    public FundSchemeRepository(MembersDbContext context) => _context = context;

    public Task<FundScheme?> FindByIdAsync(Guid schemeId)
        => _context.FundSchemes.FindAsync(schemeId).AsTask();

    public Task<List<FundScheme>> GetAllAsync()
        => _context.FundSchemes.ToListAsync();

    public Task<FundScheme?> GetFirstAsync()
        => _context.FundSchemes.FirstOrDefaultAsync();

    public async Task AddAsync(FundScheme scheme)
        => await _context.FundSchemes.AddAsync(scheme);
}




