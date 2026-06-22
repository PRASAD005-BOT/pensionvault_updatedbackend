using PensionVault.Application.DTOs.Schemes;

namespace PensionVault.Application.Interfaces;

public interface ISchemeService
{
    Task<IEnumerable<SchemeResponse>> GetAllAsync();
    Task<SchemeResponse> GetByIdAsync(Guid id);
    Task<SchemeResponse> CreateAsync(CreateSchemeRequest request);
    Task<SchemeResponse> UpdateAsync(Guid id, UpdateSchemeRequest request);
}
