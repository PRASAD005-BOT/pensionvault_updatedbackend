using Contributions.Domain.Repositories;
using Contributions.Services.HttpClients;
using Contributions.Domain.Entities;

namespace Contributions.Services;

public class ReportService : IReportService
{
    private readonly IContributionRepository _contributionRepo;
    private readonly MemberServiceClient _memberClient;

    public ReportService(
        IContributionRepository contributionRepo,
        MemberServiceClient memberClient)
    {
        _contributionRepo = contributionRepo;
        _memberClient = memberClient;
    }

    public async Task<IEnumerable<object>> GetContributionDefaultsAsync(string? period = null)
    {
        var defaults = await _contributionRepo.GetByStatusesAsync(
            RemittanceStatus.Default, RemittanceStatus.Shortfall);

        if (!string.IsNullOrEmpty(period))
            defaults = defaults.Where(r => r.RemittancePeriod == period).ToList();

        var employers = await _memberClient.GetAllEmployersAsync();
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
}



