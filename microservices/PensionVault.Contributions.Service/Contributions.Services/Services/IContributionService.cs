using Contributions.Domain.Repositories;
using Contributions.Services.DTOs;

namespace Contributions.Services;

public interface IContributionService
{
    Task<RemittanceResponse> CreateRemittanceAsync(CreateRemittanceRequest request);
    Task<RemittanceResponse> GetRemittanceAsync(Guid remittanceId);
    Task<IEnumerable<RemittanceResponse>> GetEmployerRemittancesAsync(Guid employerId);
    Task<RemittanceResponse> ReconcileAsync(Guid remittanceId);
    Task<IEnumerable<MemberContributionResponse>> GetMemberContributionsAsync(Guid memberId);
    Task<IEnumerable<RemittanceResponse>> GetAllRemittancesAsync();
    Task<ReconciliationReportResponse> GetReconciliationReportAsync(Guid remittanceId);
    Task<IEnumerable<RemittanceResponse>> GetDefaultersAsync();
    Task<IEnumerable<RemittanceResponse>> GetOverdueRemittancesAsync();
    Task<DefaulterSummaryResponse> GetDefaulterSummaryAsync(Guid employerId);
    Task<IEnumerable<MemberShortfallResponse>> GetMemberShortfallsAsync(Guid memberId);
    Task<ShortfallRequestResponse> RaiseShortfallAsync(Guid memberId, CreateShortfallRequest request);
    Task<ShortfallRequestResponse> ResolveShortfallAsync(Guid shortfallRequestId, ResolveShortfallRequest request);
    Task<ShortfallRequestResponse> RejectShortfallAsync(Guid shortfallRequestId, RejectShortfallRequest request);
    Task<IEnumerable<ShortfallRequestResponse>> GetAllShortfallRequestsAsync();
    Task<IEnumerable<ShortfallRequestResponse>> GetShortfallRequestsByEmployerAsync(Guid employerId);
    Task<IEnumerable<ShortfallRequestResponse>> GetShortfallRequestsByMemberAsync(Guid memberId);
}



