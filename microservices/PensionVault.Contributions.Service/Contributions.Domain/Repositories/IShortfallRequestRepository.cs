using Contributions.Domain.Entities;

namespace Contributions.Domain.Repositories;

public interface IShortfallRequestRepository
{
    Task<ShortfallRequest?> FindByIdAsync(Guid shortfallRequestId);
    Task<List<ShortfallRequest>> GetAllAsync();
    Task<List<ShortfallRequest>> GetByEmployerAsync(Guid employerId);
    Task<List<ShortfallRequest>> GetByMemberAsync(Guid memberId);
    Task AddAsync(ShortfallRequest request);
}
