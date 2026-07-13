using Members.Domain.Entities;

namespace Members.Domain.Repositories;

public interface IFundSchemeRepository
{
    Task<FundScheme?> FindByIdAsync(Guid schemeId);
    Task<List<FundScheme>> GetAllAsync();
    Task<FundScheme?> GetFirstAsync();
    Task AddAsync(FundScheme scheme);
}


