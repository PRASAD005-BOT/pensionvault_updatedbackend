using Members.Domain.Entities;
namespace Members.Domain.Repositories;

public interface IFundAccountRepository
{
    Task<List<ExternalFundAccount>> GetByMemberAsync(Guid memberId);
    Task AddAsync(ExternalFundAccount account);
}


