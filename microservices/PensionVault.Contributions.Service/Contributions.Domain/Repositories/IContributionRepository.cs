using Contributions.Domain.Entities;

namespace Contributions.Domain.Repositories;

public interface IContributionRepository
{
    Task<ContributionRemittance?> FindRemittanceByIdAsync(Guid remittanceId);
    Task<List<ContributionRemittance>> GetAllRemittancesAsync();
    Task<List<ContributionRemittance>> GetByEmployerAsync(Guid employerId);
    Task<List<ContributionRemittance>> GetByStatusesAsync(params RemittanceStatus[] statuses);
    Task<int> CountPostedContributionsAsync(Guid remittanceId);
    Task<decimal> SumReconciledAmountAsync(Guid remittanceId);
    Task<List<MemberContribution>> GetByMemberAsync(Guid memberId);
    Task AddRemittanceAsync(ContributionRemittance remittance);
    Task AddContributionAsync(MemberContribution contribution);
}


