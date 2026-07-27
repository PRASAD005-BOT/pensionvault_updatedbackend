using Contributions.Services.DTOs;
using Contributions.Domain.Entities;
using Contributions.Domain.Repositories;
using Contributions.Services.HttpClients;
using PensionVault.Shared.Contracts;

namespace Contributions.Services;

public class InvestmentService : IInvestmentService
{
    private readonly IInvestmentRepository _investmentRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MemberServiceClient _memberClient;
    private readonly NotificationServiceClient _notificationClient;

    public InvestmentService(
        IInvestmentRepository investmentRepo, 
        IUnitOfWork unitOfWork,
        MemberServiceClient memberClient,
        NotificationServiceClient notificationClient)
    {
        _investmentRepo = investmentRepo;
        _unitOfWork = unitOfWork;
        _memberClient = memberClient;
        _notificationClient = notificationClient;
    }

    public async Task<IEnumerable<PortfolioResponse>> GetPortfoliosAsync(Guid? schemeId = null)
    {
        var portfolios = await _investmentRepo.GetPortfoliosAsync(schemeId);
        var list = new List<PortfolioResponse>();
        foreach (var p in portfolios)
        {
            var scheme = await _memberClient.GetSchemeByIdAsync(p.SchemeId);
            list.Add(new PortfolioResponse(
                p.PortfolioId, p.SchemeId, scheme?.SchemeName ?? "",
                p.AssetClass, p.AllocationPercent, p.InvestedValue,
                p.CurrentValue, p.YieldEarned, p.LastUpdated));
        }
        return list;
    }

    public async Task<PortfolioResponse> CreatePortfolioAsync(CreatePortfolioRequest request)
    {
        // FundScheme is duplicated per-service; make sure the local copy exists so the
        // FK_InvestmentPortfolios_FundSchemes_SchemeId constraint holds (avoids a raw 500).
        await EnsureSchemeExistsAsync(request.SchemeId);

        // Each submission creates a distinct individual investment (no merging by asset class).
        // Existing allocations plus this new item must not exceed 100%.
        var portfolios = await _investmentRepo.GetPortfoliosAsync(request.SchemeId);
        var existingAllocation = portfolios.Sum(p => p.AllocationPercent);
        var projectedTotal = existingAllocation + request.AllocationPercent;
        if (projectedTotal > 100m)
        {
            throw new InvalidOperationException(
                $"Total allocation cannot exceed 100%. Current total with this item is {projectedTotal:0.##}%.");
        }

        var portfolio = new InvestmentPortfolio
        {
            SchemeId = request.SchemeId,
            AssetClass = request.AssetClass,
            AllocationPercent = request.AllocationPercent,
            InvestedValue = request.InvestedValue,
            CurrentValue = request.CurrentValue,
            YieldEarned = request.CurrentValue - request.InvestedValue,
            LastUpdated = DateTime.UtcNow
        };
        await _investmentRepo.AddPortfolioAsync(portfolio);

        await CreateInvestmentNotificationAsync($"New investment ({portfolio.AssetClass}) added. Invested: ₹{portfolio.InvestedValue:N2}, current value: ₹{portfolio.CurrentValue:N2}.");
        await _unitOfWork.SaveChangesAsync();

        var created = await _investmentRepo.FindPortfolioByIdAsync(portfolio.PortfolioId);
        var createdScheme = await _memberClient.GetSchemeByIdAsync(created!.SchemeId);
        return new PortfolioResponse(
            created.PortfolioId, created.SchemeId, createdScheme?.SchemeName ?? "",
            created.AssetClass, created.AllocationPercent, created.InvestedValue,
            created.CurrentValue, created.YieldEarned, created.LastUpdated);
    }

    public async Task<PortfolioResponse> UpdatePortfolioAsync(Guid portfolioId, UpdatePortfolioRequest request)
    {
        var portfolio = await _investmentRepo.FindPortfolioByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException("Portfolio not found.");
        portfolio.AllocationPercent = request.AllocationPercent;
        portfolio.InvestedValue = request.InvestedValue;
        portfolio.CurrentValue = request.CurrentValue;
        portfolio.YieldEarned = request.CurrentValue - request.InvestedValue;
        portfolio.LastUpdated = DateTime.UtcNow;

        await CreateInvestmentNotificationAsync($"Investment ({portfolio.AssetClass}) updated. Allocation: {portfolio.AllocationPercent:N2}%, current value: ₹{portfolio.CurrentValue:N2}.");
        await _unitOfWork.SaveChangesAsync();

        var updated = await _investmentRepo.FindPortfolioByIdAsync(portfolioId);
        var scheme = await _memberClient.GetSchemeByIdAsync(updated!.SchemeId);
        return new PortfolioResponse(
            updated.PortfolioId, updated.SchemeId, scheme?.SchemeName ?? "",
            updated.AssetClass, updated.AllocationPercent, updated.InvestedValue,
            updated.CurrentValue, updated.YieldEarned, updated.LastUpdated);
    }

    public async Task<IEnumerable<CorpusResponse>> GetCorpusRecordsAsync(Guid? schemeId = null)
    {
        var records = await _investmentRepo.GetCorpusRecordsAsync(schemeId);
        var list = new List<CorpusResponse>();
        foreach (var c in records)
        {
            var scheme = await _memberClient.GetSchemeByIdAsync(c.SchemeId);
            list.Add(new CorpusResponse(
                c.CorpusId, c.SchemeId, scheme?.SchemeName ?? "",
                c.RecordDate,
                c.ClosingCorpus - c.TotalContributions + c.TotalWithdrawals - c.InvestmentIncome + c.ManagementExpenses,
                c.TotalContributions, c.TotalWithdrawals,
                c.InvestmentIncome, c.ManagementExpenses, c.ClosingCorpus, c.Status));
        }
        return list;
    }

    public async Task<CorpusResponse> CreateCorpusRecordAsync(CreateCorpusRequest request)
    {
        // Same per-service FundScheme duplication applies to CorpusRecords' FK.
        await EnsureSchemeExistsAsync(request.SchemeId);

        var lastCorpus = await _investmentRepo.GetLastFinalisedCorpusAsync(request.SchemeId);
        var openingCorpus = lastCorpus?.ClosingCorpus ?? 0;

        var closingCorpus =
            openingCorpus
            + request.TotalContributions
            - request.TotalWithdrawals
            + request.InvestmentIncome
            - request.ManagementExpenses;

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
        var scheme = await _memberClient.GetSchemeByIdAsync(created!.SchemeId);
        return new CorpusResponse(
            created.CorpusId, created.SchemeId, scheme?.SchemeName ?? "",
            created.RecordDate,
            created.ClosingCorpus - created.TotalContributions + created.TotalWithdrawals - created.InvestmentIncome + created.ManagementExpenses,
            created.TotalContributions, created.TotalWithdrawals,
            created.InvestmentIncome, created.ManagementExpenses, created.ClosingCorpus, created.Status);
    }

    public async Task<CorpusResponse> FinaliseCorpusAsync(Guid corpusId)
    {
        var corpus = await _investmentRepo.FindCorpusByIdAsync(corpusId)
            ?? throw new KeyNotFoundException("Corpus record not found.");

        if (corpus.Status == CorpusStatus.Finalised)
        {
            throw new InvalidOperationException("Corpus record is already finalised.");
        }

        corpus.Status = CorpusStatus.Finalised;
        await CreateInvestmentNotificationAsync($"Corpus record for date {corpus.RecordDate:yyyy-MM-dd} has been finalised. Final closing corpus: ₹{corpus.ClosingCorpus:N2}.");
        await _unitOfWork.SaveChangesAsync();

        var scheme = await _memberClient.GetSchemeByIdAsync(corpus.SchemeId);
        return new CorpusResponse(
            corpus.CorpusId, corpus.SchemeId, scheme?.SchemeName ?? "",
            corpus.RecordDate,
            corpus.ClosingCorpus - corpus.TotalContributions + corpus.TotalWithdrawals - corpus.InvestmentIncome + corpus.ManagementExpenses,
            corpus.TotalContributions, corpus.TotalWithdrawals,
            corpus.InvestmentIncome, corpus.ManagementExpenses, corpus.ClosingCorpus, corpus.Status);
    }

    // Ensures the scheme exists in THIS service's FundSchemes table (it is duplicated across
    // services). If missing locally but known to the Members service, it is synced in; if the
    // scheme does not exist anywhere, a 400 is returned instead of a FK-violation 500.
    private async Task EnsureSchemeExistsAsync(Guid schemeId)
    {
        if (await _investmentRepo.SchemeExistsAsync(schemeId)) return;

        var schemeInfo = await _memberClient.GetSchemeByIdAsync(schemeId)
            ?? throw new ArgumentException("The selected fund scheme does not exist. Please choose a valid scheme.");

        await _investmentRepo.AddSchemeAsync(new FundScheme
        {
            SchemeId = schemeInfo.SchemeId,
            SchemeName = schemeInfo.SchemeName,
            SchemeType = Enum.TryParse<SchemeType>(schemeInfo.SchemeType, true, out var type) ? type : SchemeType.EPF,
            EmployeeContributionRate = schemeInfo.EmployeeContributionRate,
            EmployerContributionRate = schemeInfo.EmployerContributionRate,
            InterestRatePA = schemeInfo.InterestRatePA,
            VestingYears = schemeInfo.VestingYears,
            VestingPercent = schemeInfo.VestingPercent,
            Status = Enum.TryParse<SchemeStatus>(schemeInfo.Status, true, out var status) ? status : SchemeStatus.Active
        });
    }

    private async Task CreateInvestmentNotificationAsync(string message)
    {
        try
        {
            var ioUsers = await _memberClient.GetUsersByRoleAsync("InvestmentOfficer");
            var notifications = ioUsers.Select(user => new CreateNotificationRequest(
                user.UserId,
                message,
                "Investment"
            )).ToList();

            if (notifications.Any())
            {
                await _notificationClient.SendBulkNotificationsAsync(notifications);
            }
        }
        catch { }
    }
}





