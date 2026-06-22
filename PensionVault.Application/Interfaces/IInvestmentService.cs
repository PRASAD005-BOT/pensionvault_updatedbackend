using PensionVault.Application.DTOs.Investment;

namespace PensionVault.Application.Interfaces;

public interface IInvestmentService
{
    Task<IEnumerable<PortfolioResponse>> GetPortfoliosAsync(Guid? schemeId = null);
    Task<PortfolioResponse> CreatePortfolioAsync(CreatePortfolioRequest request);
    Task<PortfolioResponse> UpdatePortfolioAsync(Guid portfolioId, UpdatePortfolioRequest request);
    Task<IEnumerable<CorpusResponse>> GetCorpusRecordsAsync(Guid? schemeId = null);
    Task<CorpusResponse> CreateCorpusRecordAsync(CreateCorpusRequest request);
    Task<CorpusResponse> FinaliseCorpusAsync(Guid corpusId);
}
