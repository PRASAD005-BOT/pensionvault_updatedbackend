using Annuity.Domain.Entities;

namespace Annuity.Domain.Repositories;

public interface IAnnuityRequestRepository
{
    Task AddAsync(AnnuityRequest request);
    Task<AnnuityRequest?> FindByIdAsync(Guid requestId);
    Task<List<AnnuityRequest>> GetByMemberAsync(Guid memberId);
    Task<List<AnnuityRequest>> GetPendingAsync();
    Task<List<AnnuityRequest>> GetAllAsync();
    Task<AnnuityRequest?> FindPendingByMemberAsync(Guid memberId);
}


