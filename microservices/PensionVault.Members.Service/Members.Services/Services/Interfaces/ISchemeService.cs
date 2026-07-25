using Members.Domain.Repositories;
using Members.Services.DTOs;
using PensionVault.Shared.Contracts;

namespace Members.Services;

public interface ISchemeService
{
    Task<IEnumerable<SchemeResponse>> GetAllAsync();
    Task<SchemeResponse> GetByIdAsync(Guid id);
    Task<SchemeResponse> CreateAsync(CreateSchemeRequest request);
    Task<SchemeResponse> UpdateAsync(Guid id, UpdateSchemeRequest request);
}





