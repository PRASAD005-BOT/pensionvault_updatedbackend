using PensionVault.Domain.Entities;

namespace PensionVault.Domain.Interfaces;

public interface IEmployerRepository
{
    Task<Employer?> FindByIdAsync(Guid employerId);
    Task<List<Employer>> GetAllAsync();
    Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber);
    Task AddAsync(Employer employer);
}
