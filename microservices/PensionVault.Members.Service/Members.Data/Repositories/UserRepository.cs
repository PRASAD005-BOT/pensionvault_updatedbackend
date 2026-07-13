using Members.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Members.Domain.Entities;
using Members.Data;

namespace Members.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MembersDbContext _context;
    public UserRepository(MembersDbContext context) => _context = context;

    public Task<User?> FindByIdAsync(Guid userId)
        => _context.Users.FindAsync(userId).AsTask();

    public Task<User?> FindByEmailAsync(string email)
        => _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> FindByRefreshTokenAsync(string refreshToken)
        => _context.Users.FirstOrDefaultAsync(u =>
            u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);

    public Task<bool> ExistsByEmailAsync(string email)
        => _context.Users.AnyAsync(u => u.Email == email);

    public Task<List<User>> GetByRoleAsync(UserRole role)
        => _context.Users.Where(u => u.Role == role).ToListAsync();

    public Task<List<User>> GetByOrgAndRoleAsync(Guid organisationId, UserRole role)
        => _context.Users
            .Where(u => u.OrganisationId == organisationId && u.Role == role)
            .ToListAsync();

    public async Task AddAsync(User user)
        => await _context.Users.AddAsync(user);
}




