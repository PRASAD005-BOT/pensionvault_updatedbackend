using Members.Domain.Entities;

namespace Members.Domain.Repositories;

public interface IEmployerRepository
{
    Task<Employer?> FindByIdAsync(Guid employerId);
    Task<List<Employer>> GetAllAsync();
    Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber);
    Task<bool> ExistsByEmployerCodeAsync(string employerCode);
    Task AddAsync(Employer employer);
}


