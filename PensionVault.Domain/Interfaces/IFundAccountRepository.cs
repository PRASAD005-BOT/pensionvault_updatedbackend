using PensionVault.Domain.Entities;

namespace PensionVault.Domain.Interfaces;

public interface IFundAccountRepository
{
    Task<FundAccount?> FindByIdAsync(Guid accountId);
    Task<FundAccount?> FindActiveByMemberAsync(Guid memberId);
    Task<List<FundAccount>> GetByMemberAsync(Guid memberId);
    Task<bool> ExistsByMemberAsync(Guid memberId);
    Task AddAsync(FundAccount account);
}
