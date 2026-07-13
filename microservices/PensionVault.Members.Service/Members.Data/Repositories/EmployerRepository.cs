using Members.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Members.Domain.Entities;
using Members.Data;

namespace Members.Data.Repositories;

public class EmployerRepository : IEmployerRepository
{
    private readonly MembersDbContext _context;
    public EmployerRepository(MembersDbContext context) => _context = context;

    public Task<Employer?> FindByIdAsync(Guid employerId)
        => _context.Employers.FindAsync(employerId).AsTask();

    public Task<List<Employer>> GetAllAsync()
        => _context.Employers.ToListAsync();

    public Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber)
        => _context.Employers.AnyAsync(e => e.RegistrationNumber == registrationNumber);

    public async Task AddAsync(Employer employer)
        => await _context.Employers.AddAsync(employer);
}




