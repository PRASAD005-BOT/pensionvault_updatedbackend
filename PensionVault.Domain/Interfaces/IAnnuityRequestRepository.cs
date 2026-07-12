using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;

namespace PensionVault.Domain.Interfaces;

public interface IAnnuityRequestRepository
{
    Task AddAsync(AnnuityRequest request);
    Task<AnnuityRequest?> FindByIdAsync(Guid requestId);
    Task<List<AnnuityRequest>> GetByMemberAsync(Guid memberId);
    Task<List<AnnuityRequest>> GetPendingAsync();
    Task<List<AnnuityRequest>> GetAllAsync();
    Task<AnnuityRequest?> FindPendingByMemberAsync(Guid memberId);
}
