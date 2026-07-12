using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Annuity.Service.ProxyRepositories;

public class HttpContributionRepository : BaseHttpRepository, IContributionRepository
{
    public HttpContributionRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public async Task<List<MemberContribution>> GetByMemberAsync(Guid memberId)
        => await GetAsync<List<MemberContribution>>($"api/remittances/member/{memberId}") ?? new List<MemberContribution>();

    public Task<ContributionRemittance?> FindRemittanceByIdAsync(Guid remittanceId) => throw new NotSupportedException();
    public Task<List<ContributionRemittance>> GetAllRemittancesAsync() => throw new NotSupportedException();
    public Task<List<ContributionRemittance>> GetByEmployerAsync(Guid employerId) => throw new NotSupportedException();
    public Task<List<ContributionRemittance>> GetByStatusesAsync(params RemittanceStatus[] statuses) => throw new NotSupportedException();
    public Task<int> CountPostedContributionsAsync(Guid remittanceId) => throw new NotSupportedException();
    public Task<decimal> SumReconciledAmountAsync(Guid remittanceId) => throw new NotSupportedException();
    public Task AddRemittanceAsync(ContributionRemittance remittance) => throw new NotSupportedException();
    public Task AddContributionAsync(MemberContribution contribution) => throw new NotSupportedException();
}

