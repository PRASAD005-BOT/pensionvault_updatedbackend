using PensionVault.Application.DTOs.Investment;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Application.Services;

public class InvestmentService : IInvestmentService
{
    private readonly IInvestmentRepository _investmentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public InvestmentService(IInvestmentRepository investmentRepo, IUnitOfWork unitOfWork)
    {
        _investmentRepo = investmentRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PortfolioResponse>> GetPortfoliosAsync(Guid? schemeId = null)
    {
        var portfolios = await _investmentRepo.GetPortfoliosAsync(schemeId);
        return portfolios.Select(ToPortfolioResponse);
    }

    public async Task<PortfolioResponse> CreatePortfolioAsync(CreatePortfolioRequest request)
    {
        var portfolio = new InvestmentPortfolio
        {
            SchemeId = request.SchemeId,
            AssetClass = request.AssetClass,
            AllocationPercent = request.AllocationPercent,
            InvestedValue = request.InvestedValue,
            CurrentValue = request.CurrentValue,
            YieldEarned = request.YieldEarned,
            LastUpdated = DateTime.UtcNow
        };
        await _investmentRepo.AddPortfolioAsync(portfolio);
        await _unitOfWork.SaveChangesAsync();

        var created = await _investmentRepo.FindPortfolioByIdAsync(portfolio.PortfolioId);
        return ToPortfolioResponse(created!);
    }

    public async Task<PortfolioResponse> UpdatePortfolioAsync(Guid portfolioId, UpdatePortfolioRequest request)
    {
        var portfolio = await _investmentRepo.FindPortfolioByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException("Portfolio not found.");
        portfolio.AllocationPercent = request.AllocationPercent;
        portfolio.InvestedValue = request.InvestedValue;
        portfolio.CurrentValue = request.CurrentValue;
        portfolio.YieldEarned = request.YieldEarned;
        portfolio.LastUpdated = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return ToPortfolioResponse(portfolio);
    }

    public async Task<IEnumerable<CorpusResponse>> GetCorpusRecordsAsync(Guid? schemeId = null)
    {
        var records = await _investmentRepo.GetCorpusRecordsAsync(schemeId);
        return records.Select(ToCorpusResponse);
    }

    public async Task<CorpusResponse> CreateCorpusRecordAsync(CreateCorpusRequest request)
    {
        var lastCorpus = await _investmentRepo.GetLastFinalisedCorpusAsync(request.SchemeId);
        var openingCorpus = lastCorpus?.ClosingCorpus ?? 0;

        var closingCorpus =
            openingCorpus
            + request.TotalContributions
            - request.TotalWithdrawals
            + request.InvestmentIncome
            - request.ManagementExpenses;

        // Prevent negative closing corpus
        if (closingCorpus < 0)
        {
            throw new InvalidOperationException("Closing corpus cannot be negative.");
        }

        var corpus = new CorpusRecord
        {
            SchemeId = request.SchemeId,
            RecordDate = request.RecordDate,
            TotalContributions = request.TotalContributions,
            TotalWithdrawals = request.TotalWithdrawals,
            InvestmentIncome = request.InvestmentIncome,
            ManagementExpenses = request.ManagementExpenses,
            ClosingCorpus = closingCorpus,
            Status = CorpusStatus.Draft
        };

        await _investmentRepo.AddCorpusAsync(corpus);
        await _unitOfWork.SaveChangesAsync();

        var created = await _investmentRepo.FindCorpusByIdAsync(corpus.CorpusId);
        return ToCorpusResponse(created!);
    }

    public async Task<CorpusResponse> FinaliseCorpusAsync(Guid corpusId)
    {
        var corpus = await _investmentRepo.FindCorpusByIdAsync(corpusId)
            ?? throw new KeyNotFoundException("Corpus record not found.");

        // Prevent double finalisation
        if (corpus.Status == CorpusStatus.Finalised)
        {
            throw new InvalidOperationException("Corpus record is already finalised.");
        }

        corpus.Status = CorpusStatus.Finalised;

        await _unitOfWork.SaveChangesAsync();

        return ToCorpusResponse(corpus);
    }

    private static PortfolioResponse ToPortfolioResponse(InvestmentPortfolio p) => new(
        p.PortfolioId, p.SchemeId, p.Scheme?.SchemeName ?? "",
        p.AssetClass, p.AllocationPercent, p.InvestedValue,
        p.CurrentValue, p.YieldEarned, p.LastUpdated);

    private static CorpusResponse ToCorpusResponse(CorpusRecord c) => new(
        c.CorpusId, c.SchemeId, c.Scheme?.SchemeName ?? "",
        c.RecordDate,
        c.ClosingCorpus - c.TotalContributions + c.TotalWithdrawals - c.InvestmentIncome + c.ManagementExpenses,
        c.TotalContributions, c.TotalWithdrawals,
        c.InvestmentIncome, c.ManagementExpenses, c.ClosingCorpus, c.Status);
}
