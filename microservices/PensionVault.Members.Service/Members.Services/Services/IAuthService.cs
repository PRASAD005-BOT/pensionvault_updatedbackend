using Members.Domain.Repositories;
using Members.Services.DTOs;
using PensionVault.Shared.Contracts;

namespace Members.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
}





