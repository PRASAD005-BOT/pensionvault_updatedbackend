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
    private readonly INotificationRepository _notificationRepo;
    private readonly IUserRepository _userRepo;

    public InvestmentService(
        IInvestmentRepository investmentRepo, 
        IUnitOfWork unitOfWork,
        INotificationRepository notificationRepo,
        IUserRepository userRepo)
    {
        _investmentRepo = investmentRepo;
        _unitOfWork = unitOfWork;
        _notificationRepo = notificationRepo;
        _userRepo = userRepo;
    }

    public async Task<IEnumerable<PortfolioResponse>> GetPortfoliosAsync(Guid? schemeId = null)
    {
        var portfolios = await _investmentRepo.GetPortfoliosAsync(schemeId);
        return portfolios.Select(ToPortfolioResponse);
    }

    public async Task<PortfolioResponse> CreatePortfolioAsync(CreatePortfolioRequest request)
    {
        var portfolios = await _investmentRepo.GetPortfoliosAsync(request.SchemeId);
        var existing = portfolios.FirstOrDefault(p => p.AssetClass == request.AssetClass);

        if (existing != null)
        {
            existing.InvestedValue += request.InvestedValue;
            existing.CurrentValue += request.CurrentValue;
            existing.YieldEarned += request.YieldEarned;
            existing.LastUpdated = DateTime.UtcNow;

            await RecalculateAllocationsAsync(request.SchemeId);

            await CreateInvestmentNotificationAsync($"Investment added to existing fund for asset class {existing.AssetClass}. Added: ₹{request.InvestedValue:N2}, new total current value: ₹{existing.CurrentValue:N2}.");
            await _unitOfWork.SaveChangesAsync();

            var updated = await _investmentRepo.FindPortfolioByIdAsync(existing.PortfolioId);
            return ToPortfolioResponse(updated!);
        }

        var portfolio = new InvestmentPortfolio
        {
            SchemeId = request.SchemeId,
            AssetClass = request.AssetClass,
            InvestedValue = request.InvestedValue,
            CurrentValue = request.CurrentValue,
            YieldEarned = request.YieldEarned,
            LastUpdated = DateTime.UtcNow
        };
        await _investmentRepo.AddPortfolioAsync(portfolio);
        await _unitOfWork.SaveChangesAsync();

        await RecalculateAllocationsAsync(request.SchemeId);

        await CreateInvestmentNotificationAsync($"New portfolio created for asset class {portfolio.AssetClass}.");
        await _unitOfWork.SaveChangesAsync();

        var created = await _investmentRepo.FindPortfolioByIdAsync(portfolio.PortfolioId);
        return ToPortfolioResponse(created!);
    }

    public async Task<PortfolioResponse> UpdatePortfolioAsync(Guid portfolioId, UpdatePortfolioRequest request)
    {
        var portfolio = await _investmentRepo.FindPortfolioByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException("Portfolio not found.");
        portfolio.InvestedValue = request.InvestedValue;
        portfolio.CurrentValue = request.CurrentValue;
        portfolio.YieldEarned = request.YieldEarned;
        portfolio.LastUpdated = DateTime.UtcNow;

        await RecalculateAllocationsAsync(portfolio.SchemeId);

        await CreateInvestmentNotificationAsync($"Portfolio for asset class {portfolio.AssetClass} updated. Current value: ₹{portfolio.CurrentValue:N2}, Yield: ₹{portfolio.YieldEarned:N2}.");
        await _unitOfWork.SaveChangesAsync();
        
        var updated = await _investmentRepo.FindPortfolioByIdAsync(portfolioId);
        return ToPortfolioResponse(updated!);
    }

    private async Task RecalculateAllocationsAsync(Guid schemeId)
    {
        var portfolios = await _investmentRepo.GetPortfoliosAsync(schemeId);
        decimal totalCurrentValue = portfolios.Sum(p => p.CurrentValue);

        if (totalCurrentValue > 0)
        {
            foreach (var p in portfolios)
            {
                p.AllocationPercent = Math.Round((p.CurrentValue / totalCurrentValue) * 100, 2);
            }
        }
        else if (portfolios.Any())
        {
            decimal equalShare = Math.Round(100.00m / portfolios.Count, 2);
            foreach (var p in portfolios)
            {
                p.AllocationPercent = equalShare;
            }
        }
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
        await CreateInvestmentNotificationAsync($"New draft corpus record created for date {corpus.RecordDate:yyyy-MM-dd}. Closing corpus: ₹{corpus.ClosingCorpus:N2}.");
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
        await CreateInvestmentNotificationAsync($"Corpus record for date {corpus.RecordDate:yyyy-MM-dd} has been finalised. Final closing corpus: ₹{corpus.ClosingCorpus:N2}.");
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

    private async Task CreateInvestmentNotificationAsync(string message)
    {
        var ioUsers = await _userRepo.GetByRoleAsync(UserRole.InvestmentOfficer);
        var notifications = ioUsers.Select(user => new Notification
        {
            UserId = user.UserId,
            Message = message,
            Category = NotificationCategory.Investment,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        }).ToList();

        if (notifications.Any())
        {
            await _notificationRepo.AddRangeAsync(notifications);
        }
    }
}
