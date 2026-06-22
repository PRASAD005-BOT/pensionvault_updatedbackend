using PensionVault.Domain.Enums;

namespace PensionVault.Application.Interfaces;

public interface IReportService
{
    Task<IEnumerable<object>> GetContributionDefaultsAsync();
    Task<IEnumerable<object>> GetAuditTrailAsync(string? entityType, DateTime? from, DateTime? to);
    Task<IEnumerable<object>> GetStatutoryReturnsAsync(string? period);
}
