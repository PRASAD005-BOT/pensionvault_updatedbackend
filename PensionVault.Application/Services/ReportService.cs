using PensionVault.Application.Interfaces;
using PensionVault.Domain.Interfaces;
using PensionVault.Domain.Enums;

namespace PensionVault.Application.Services;

public class ReportService : IReportService
{
    private readonly IContributionRepository _contributionRepo;
    private readonly IEmployerRepository _employerRepo;
    private readonly IFundSchemeRepository _schemeRepo;
    private readonly IFundAccountRepository _accountRepo;
    private readonly IAnnuityRepository _annuityRepo;
    private readonly ILedgerRepository _ledgerRepo;

    public ReportService(
        IContributionRepository contributionRepo,
        IEmployerRepository employerRepo,
        IFundSchemeRepository schemeRepo,
        IFundAccountRepository accountRepo,
        IAnnuityRepository annuityRepo,
        ILedgerRepository ledgerRepo)
    {
        _contributionRepo = contributionRepo;
        _employerRepo = employerRepo;
        _schemeRepo = schemeRepo;
        _accountRepo = accountRepo;
        _annuityRepo = annuityRepo;
        _ledgerRepo = ledgerRepo;
    }

    public async Task<IEnumerable<object>> GetContributionDefaultsAsync()
    {
        var defaults = await _contributionRepo.GetByStatusesAsync(
            RemittanceStatus.Default, RemittanceStatus.Shortfall);

        var employers = await _employerRepo.GetAllAsync();
        var employerDict = employers.ToDictionary(e => e.EmployerId, e => e.CompanyName);

        return defaults.Select(r => (object)new
        {
            r.RemittanceId, r.EmployerId,
            EmployerName = employerDict.TryGetValue(r.EmployerId, out var name) ? name : "",
            r.RemittancePeriod, r.TotalAmount,
            Status = r.Status.ToString(), r.RemittanceDate
        });
    }

    public async Task<IEnumerable<object>> GetStatutoryReturnsAsync(string? period)
    {
        var all = await _contributionRepo.GetAllRemittancesAsync();
        var filtered = string.IsNullOrEmpty(period)
            ? all
            : all.Where(r => r.RemittancePeriod == period).ToList();

        return filtered
            .GroupBy(r => r.RemittancePeriod)
            .Select(g => (object)new
            {
                Period = g.Key,
                TotalEmployers = g.Count(),
                TotalEmployeeShare = g.Sum(r => r.TotalEmployeeShare),
                TotalEmployerShare = g.Sum(r => r.TotalEmployerShare),
                TotalAmount = g.Sum(r => r.TotalAmount),
                TotalCoveredMembers = g.Sum(r => r.CoverageCount)
            })
            .OrderByDescending(x => ((dynamic)x).Period);
    }

    // Audit trail is kept in the controller since it needs raw AuditLog + User join
    // that would require a dedicated IAuditLogRepository. For now it still uses
    // IAppDbContext via the controller — a future step can extract it too.
    public Task<IEnumerable<object>> GetAuditTrailAsync(string? entityType, DateTime? from, DateTime? to)
        => throw new NotSupportedException(
            "Audit trail report is handled directly by ReportsController via AppDbContext.");
}
