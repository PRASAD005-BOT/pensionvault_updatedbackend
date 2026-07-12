using Microsoft.AspNetCore.Http;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;
using PensionVault.Shared.Http;

namespace PensionVault.Claims.Service.ProxyRepositories;

public class HttpUserRepository : BaseHttpRepository, IUserRepository
{
    public HttpUserRepository(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor) { }

    public Task<User?> FindByIdAsync(Guid userId)
        => GetAsync<User>($"api/users/{userId}");

    public Task<User?> FindByEmailAsync(string email) => throw new NotSupportedException();
    public Task<User?> FindByRefreshTokenAsync(string refreshToken) => throw new NotSupportedException();
    public Task<bool> ExistsByEmailAsync(string email) => throw new NotSupportedException();
    public Task<List<User>> GetByRoleAsync(UserRole role) => throw new NotSupportedException();
    public Task<List<User>> GetByOrgAndRoleAsync(Guid organisationId, UserRole role) => throw new NotSupportedException();
    public Task AddAsync(User user) => throw new NotSupportedException();
}
