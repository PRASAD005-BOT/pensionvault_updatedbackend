using Members.Domain.Repositories;
using Members.Services.DTOs;
using PensionVault.Shared.Contracts;

namespace Members.Services;

public interface IEmployerService
{
    Task<IEnumerable<EmployerResponse>> GetAllAsync();
    Task<EmployerResponse> GetByIdAsync(Guid id);
    Task<EmployerResponse> GetByUserIdAsync(Guid userId);
    Task<EmployerResponse> CreateAsync(CreateEmployerRequest request);
    Task<EmployerResponse> UpdateAsync(Guid id, UpdateEmployerRequest request);
    Task<EmployerResponse> ApproveAsync(Guid id);
    Task<EmployerResponse> RejectAsync(Guid id);
}





