using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Contributions.Service.ProxyRepositories;

/// <summary>
/// Stub implementation - Contributions Service does not need to write/read users directly.
/// Implemented so ContributionService DI chain resolves; the actual methods are not called
/// since ContributionService only uses _userRepo in notification look-ups that are handled
/// by INotificationRepository in this service.
/// </summary>
public class HttpUserRepository : BaseHttpRepository, IUserRepository
{
    public HttpUserRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public Task<User?> FindByIdAsync(Guid userId)
        => GetAsync<User>($"/api/users/{userId}");

    public Task<User?> FindByEmailAsync(string email)
        => GetAsync<User>($"/api/users/by-email/{Uri.EscapeDataString(email)}");

    public Task<User?> FindByRefreshTokenAsync(string refreshToken)
        => Task.FromResult<User?>(null);

    public Task<bool> ExistsByEmailAsync(string email)
        => Task.FromResult(false);

    public async Task<List<User>> GetByRoleAsync(UserRole role)
    {
        var users = await GetAsync<List<User>>($"/api/users/by-role/{role}");
        return users ?? new List<User>();
    }

    public async Task<List<User>> GetByOrgAndRoleAsync(Guid organisationId, UserRole role)
    {
        if (role == UserRole.Employer)
        {
            var users = await GetAsync<List<User>>($"/api/users/employer-representatives/{organisationId}");
            return users ?? new List<User>();
        }
        return new List<User>();
    }

    public Task AddAsync(User user)
        => Task.CompletedTask;
}

