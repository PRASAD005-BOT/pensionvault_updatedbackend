using Contributions.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Contributions.Services.DTOs;

public record CreatePortfolioRequest(
    [Required(ErrorMessage = "Scheme Id is required.")]
    Guid SchemeId,

    [Required(ErrorMessage = "Asset Class is required.")]
    AssetClass AssetClass,

    [Range(0, 100, ErrorMessage = "Allocation Percent must be between 0 and 100.")]
    decimal AllocationPercent,

    [Range(0, double.MaxValue, ErrorMessage = "Invested Value cannot be negative.")]
    decimal InvestedValue,

    [Range(0, double.MaxValue, ErrorMessage = "Current Value cannot be negative.")]
    decimal CurrentValue,

    [Range(0, double.MaxValue, ErrorMessage = "Yield Earned cannot be negative.")]
    decimal YieldEarned
);

public record UpdatePortfolioRequest(
    [Range(0, 100, ErrorMessage = "Allocation Percent must be between 0 and 100.")]
    decimal AllocationPercent,

    [Range(0, double.MaxValue, ErrorMessage = "Invested Value cannot be negative.")]
    decimal InvestedValue,

    [Range(0, double.MaxValue, ErrorMessage = "Current Value cannot be negative.")]
    decimal CurrentValue,

    [Range(0, double.MaxValue, ErrorMessage = "Yield Earned cannot be negative.")]
    decimal YieldEarned
);

public record PortfolioResponse(
    Guid PortfolioId,
    Guid SchemeId,
    string SchemeName,
    AssetClass AssetClass,
    decimal AllocationPercent,
    decimal InvestedValue,
    decimal CurrentValue,
    decimal YieldEarned,
    DateTime LastUpdated
);

public record CreateCorpusRequest(
    [Required(ErrorMessage = "Scheme Id is required.")]
    Guid SchemeId,

    [Required(ErrorMessage = "Record Date is required.")]
    DateTime RecordDate,

    [Range(0, double.MaxValue, ErrorMessage = "Total Contributions cannot be negative.")]
    decimal TotalContributions,

    [Range(0, double.MaxValue, ErrorMessage = "Total Withdrawals cannot be negative.")]
    decimal TotalWithdrawals,

    [Range(0, double.MaxValue, ErrorMessage = "Investment Income cannot be negative.")]
    decimal InvestmentIncome,

    [Range(0, double.MaxValue, ErrorMessage = "Management Expenses cannot be negative.")]
    decimal ManagementExpenses
);

public record CorpusResponse(
    Guid CorpusId,
    Guid SchemeId,
    string SchemeName,
    DateTime RecordDate,
    decimal OpeningCorpus,
    decimal TotalContributions,
    decimal TotalWithdrawals,
    decimal InvestmentIncome,
    decimal ManagementExpenses,
    decimal ClosingCorpus,
    CorpusStatus Status
);


