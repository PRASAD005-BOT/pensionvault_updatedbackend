using Contributions.Domain.Repositories;
namespace Contributions.Services;

public interface IReportService
{
    Task<IEnumerable<object>> GetContributionDefaultsAsync(string? period = null);
    Task<IEnumerable<object>> GetStatutoryReturnsAsync(string? period);
}



