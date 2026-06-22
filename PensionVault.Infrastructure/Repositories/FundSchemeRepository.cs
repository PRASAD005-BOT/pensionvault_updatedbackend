using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Repositories;

public class FundSchemeRepository : IFundSchemeRepository
{
    private readonly AppDbContext _context;
    public FundSchemeRepository(AppDbContext context) => _context = context;

    public Task<FundScheme?> FindByIdAsync(Guid schemeId)
        => _context.FundSchemes.FindAsync(schemeId).AsTask();

    public Task<List<FundScheme>> GetAllAsync()
        => _context.FundSchemes.ToListAsync();

    public Task<FundScheme?> GetFirstAsync()
        => _context.FundSchemes.FirstOrDefaultAsync();

    public async Task AddAsync(FundScheme scheme)
        => await _context.FundSchemes.AddAsync(scheme);
}
