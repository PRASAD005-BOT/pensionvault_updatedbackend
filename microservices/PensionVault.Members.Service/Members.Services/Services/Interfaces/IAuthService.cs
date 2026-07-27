using Members.Domain.Repositories;
using Members.Services.DTOs;
using PensionVault.Shared.Contracts;
using PensionVault.Shared.Results;

namespace Members.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ServiceResult<AuthResponse>> RefreshTokenAsync(string refreshToken);
}