using PensionVault.Application.DTOs.Employers;

namespace PensionVault.Application.Interfaces;

public interface IEmployerService
{
    Task<IEnumerable<EmployerResponse>> GetAllAsync();
    Task<EmployerResponse> GetByIdAsync(Guid id);
    Task<EmployerResponse> GetByUserIdAsync(Guid userId);
    Task<EmployerResponse> CreateAsync(CreateEmployerRequest request);
    Task<EmployerResponse> UpdateAsync(Guid id, UpdateEmployerRequest request);
}
