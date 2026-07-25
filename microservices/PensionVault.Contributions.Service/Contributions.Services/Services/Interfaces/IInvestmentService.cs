using Contributions.Domain.Repositories;
using Contributions.Services.DTOs;

namespace Contributions.Services;

public interface IInvestmentService
{
    Task<IEnumerable<PortfolioResponse>> GetPortfoliosAsync(Guid? schemeId = null);
    Task<PortfolioResponse> CreatePortfolioAsync(CreatePortfolioRequest request);
    Task<PortfolioResponse> UpdatePortfolioAsync(Guid portfolioId, UpdatePortfolioRequest request);
    Task<IEnumerable<CorpusResponse>> GetCorpusRecordsAsync(Guid? schemeId = null);
    Task<CorpusResponse> CreateCorpusRecordAsync(CreateCorpusRequest request);
    Task<CorpusResponse> FinaliseCorpusAsync(Guid corpusId);
}



