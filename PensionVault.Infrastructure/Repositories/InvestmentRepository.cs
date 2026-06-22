using Microsoft.EntityFrameworkCore;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Infrastructure.Repositories;

public class InvestmentRepository : IInvestmentRepository
{
    private readonly AppDbContext _context;
    public InvestmentRepository(AppDbContext context) => _context = context;

    public Task<InvestmentPortfolio?> FindPortfolioByIdAsync(Guid portfolioId)
        => _context.InvestmentPortfolios
            .Include(p => p.Scheme)
            .FirstOrDefaultAsync(p => p.PortfolioId == portfolioId);

    public Task<List<InvestmentPortfolio>> GetPortfoliosAsync(Guid? schemeId = null)
    {
        var query = _context.InvestmentPortfolios.Include(p => p.Scheme).AsQueryable();
        if (schemeId.HasValue) query = query.Where(p => p.SchemeId == schemeId);
        return query.ToListAsync();
    }

    public async Task AddPortfolioAsync(InvestmentPortfolio portfolio)
        => await _context.InvestmentPortfolios.AddAsync(portfolio);

    public Task<CorpusRecord?> FindCorpusByIdAsync(Guid corpusId)
        => _context.CorpusRecords
            .Include(c => c.Scheme)
            .FirstOrDefaultAsync(c => c.CorpusId == corpusId);

    public Task<List<CorpusRecord>> GetCorpusRecordsAsync(Guid? schemeId = null)
    {
        var query = _context.CorpusRecords.Include(c => c.Scheme).AsQueryable();
        if (schemeId.HasValue) query = query.Where(c => c.SchemeId == schemeId);
        return query.OrderByDescending(c => c.RecordDate).ToListAsync();
    }

    public Task<CorpusRecord?> GetLastFinalisedCorpusAsync(Guid schemeId)
        => _context.CorpusRecords
            .Where(c => c.SchemeId == schemeId && c.Status == CorpusStatus.Finalised)
            .OrderByDescending(c => c.RecordDate)
            .FirstOrDefaultAsync();

    public async Task AddCorpusAsync(CorpusRecord corpus)
        => await _context.CorpusRecords.AddAsync(corpus);
}
