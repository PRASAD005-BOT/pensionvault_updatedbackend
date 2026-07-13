using Members.Domain.Entities;
namespace Members.Domain.Repositories;

public interface IContributionRepository
{
    Task<List<ExternalContribution>> GetByMemberAsync(Guid memberId);
}


