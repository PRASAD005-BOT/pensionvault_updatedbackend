using PensionVault.Domain.Entities;

namespace PensionVault.Domain.Interfaces;

public interface IInvestmentRepository
{
    Task<InvestmentPortfolio?> FindPortfolioByIdAsync(Guid portfolioId);
    Task<List<InvestmentPortfolio>> GetPortfoliosAsync(Guid? schemeId = null);
    Task AddPortfolioAsync(InvestmentPortfolio portfolio);
    Task<CorpusRecord?> FindCorpusByIdAsync(Guid corpusId);
    Task<List<CorpusRecord>> GetCorpusRecordsAsync(Guid? schemeId = null);
    Task<CorpusRecord?> GetLastFinalisedCorpusAsync(Guid schemeId);
    Task AddCorpusAsync(CorpusRecord corpus);
}
