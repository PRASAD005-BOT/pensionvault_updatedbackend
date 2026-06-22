using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;

namespace PensionVault.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid userId);
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByRefreshTokenAsync(string refreshToken);
    Task<bool> ExistsByEmailAsync(string email);
    Task<List<User>> GetByRoleAsync(UserRole role);
    Task<List<User>> GetByOrgAndRoleAsync(Guid organisationId, UserRole role);
    Task AddAsync(User user);
}
