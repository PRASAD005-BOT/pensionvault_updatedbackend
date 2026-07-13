using Contributions.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Contributions.Domain.Entities;
using Contributions.Data;

namespace Contributions.Data.Repositories;

public class InvestmentRepository : IInvestmentRepository
{
    private readonly ContributionsDbContext _context;
    public InvestmentRepository(ContributionsDbContext context) => _context = context;

    public Task<InvestmentPortfolio?> FindPortfolioByIdAsync(Guid portfolioId)
        => _context.InvestmentPortfolios
            .Include(p => p.Scheme)
            .FirstOrDefaultAsync(p => p.PortfolioId == portfolioId);

    public Task<List<InvestmentPortfolio>> GetPortfoliosAsync(Guid? schemeId = null)
    {
        var query = _context.InvestmentPortfolios.Include(p => p.Scheme).AsQueryable();
        if (schemeId.HasValue)
            query = query.Where(p => p.SchemeId == schemeId.Value);
        return query.ToListAsync();
    }

    public async Task AddPortfolioAsync(InvestmentPortfolio portfolio)
    {
        if (portfolio.Scheme != null)
        {
            var trackedScheme = _context.FundSchemes.Local.FirstOrDefault(s => s.SchemeId == portfolio.Scheme.SchemeId);
            if (trackedScheme != null)
            {
                portfolio.Scheme = null;
            }
            else
            {
                _context.Entry(portfolio.Scheme).State = EntityState.Unchanged;
            }
        }
        await _context.InvestmentPortfolios.AddAsync(portfolio);
    }

    public Task<CorpusRecord?> FindCorpusByIdAsync(Guid corpusId)
        => _context.CorpusRecords
            .Include(c => c.Scheme)
            .FirstOrDefaultAsync(c => c.CorpusId == corpusId);

    public Task<List<CorpusRecord>> GetCorpusRecordsAsync(Guid? schemeId = null)
    {
        var query = _context.CorpusRecords.Include(c => c.Scheme).AsQueryable();
        if (schemeId.HasValue)
            query = query.Where(c => c.SchemeId == schemeId.Value);
        return query.OrderByDescending(c => c.RecordDate).ToListAsync();
    }

    public Task<CorpusRecord?> GetLastFinalisedCorpusAsync(Guid schemeId)
        => _context.CorpusRecords
            .Where(c => c.SchemeId == schemeId && c.Status == CorpusStatus.Finalised)
            .OrderByDescending(c => c.RecordDate)
            .FirstOrDefaultAsync();

    public async Task AddCorpusAsync(CorpusRecord corpus)
    {
        if (corpus.Scheme != null)
        {
            var trackedScheme = _context.FundSchemes.Local.FirstOrDefault(s => s.SchemeId == corpus.Scheme.SchemeId);
            if (trackedScheme != null)
            {
                corpus.Scheme = null;
            }
            else
            {
                _context.Entry(corpus.Scheme).State = EntityState.Unchanged;
            }
        }
        await _context.CorpusRecords.AddAsync(corpus);
    }
}




