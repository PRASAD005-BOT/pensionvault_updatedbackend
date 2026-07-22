using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Members.Data;
using Members.Domain.Entities;
using Members.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Members.Data.Repositories;

public class EmployerRepository : IEmployerRepository
{
    private readonly MembersDbContext _context;

    public EmployerRepository(MembersDbContext context)
    {
        _context = context;
    }

    public Task<Employer?> FindByIdAsync(Guid employerId)
        => _context.Employers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.EmployerId == employerId);

    // .IgnoreQueryFilters() prevents EF Core from filtering out Deactive/Deregistered employers
    public Task<List<Employer>> GetAllAsync()
        => _context.Employers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();

    public Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber)
        => _context.Employers
            .IgnoreQueryFilters()
            .AnyAsync(e => e.RegistrationNumber == registrationNumber);

    public Task<bool> ExistsByEmployerCodeAsync(string employerCode)
        => _context.Employers
            .IgnoreQueryFilters()
            .AnyAsync(e => e.EmployerCode == employerCode);

    public async Task AddAsync(Employer employer)
        => await _context.Employers.AddAsync(employer);
}