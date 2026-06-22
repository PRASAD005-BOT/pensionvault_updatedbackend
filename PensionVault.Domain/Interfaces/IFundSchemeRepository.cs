using PensionVault.Domain.Entities;

namespace PensionVault.Domain.Interfaces;

public interface IFundSchemeRepository
{
    Task<FundScheme?> FindByIdAsync(Guid schemeId);
    Task<List<FundScheme>> GetAllAsync();
    Task<FundScheme?> GetFirstAsync();
    Task AddAsync(FundScheme scheme);
}
