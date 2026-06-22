using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PensionVault.Application.DTOs.Auth;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IEmployerRepository _employerRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;

    public AuthService(
        IUserRepository userRepo,
        IEmployerRepository employerRepo,
        INotificationRepository notificationRepo,
        IUnitOfWork unitOfWork,
        IConfiguration config)
    {
        _userRepo = userRepo;
        _employerRepo = employerRepo;
        _notificationRepo = notificationRepo;
        _unitOfWork = unitOfWork;
        _config = config;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (user.Status != UserStatus.Active)
            throw new UnauthorizedAccessException("Account is not active.");

        return await GenerateTokensAsync(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepo.ExistsByEmailAsync(request.Email))
            throw new InvalidOperationException("Email already registered.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new ArgumentException("Invalid role specified.");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            OrganisationId = request.OrganisationId,
            EmployeeId = request.EmployeeId,
            Status = UserStatus.Active
        };

        if (role == UserRole.Employer && user.OrganisationId == null)
        {
            var newEmployer = new Employer
            {
                CompanyName = request.Name + " Corporation",
                RegistrationNumber = "REG-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                ContactDetails = request.Email,
                Status = EmployerStatus.Active
            };
            await _employerRepo.AddAsync(newEmployer);
            await _unitOfWork.SaveChangesAsync();
            user.OrganisationId = newEmployer.EmployerId;
        }

        await _userRepo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        if (role == UserRole.Member)
        {
            var adminUsers = await _userRepo.GetByRoleAsync(UserRole.Admin);
            var notifications = adminUsers.Select(adminUser => new Notification
            {
                UserId = adminUser.UserId,
                Message = $"New employee registered: {user.Name} ({user.Email}). User ID: {user.UserId}",
                Category = NotificationCategory.Compliance,
                Status = NotificationStatus.Unread
            });
            await _notificationRepo.AddRangeAsync(notifications);
            if (adminUsers.Any())
                await _unitOfWork.SaveChangesAsync();
        }

        return await GenerateTokensAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepo.FindByRefreshTokenAsync(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        return await GenerateTokensAsync(user);
    }

    private async Task<AuthResponse> GenerateTokensAsync(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "PensionVault";
        var audience = _config["Jwt:Audience"] ?? "PensionVaultUsers";
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("name", user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.OrganisationId.HasValue)
            claims.Add(new Claim("OrganisationId", user.OrganisationId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(expireMinutes);

        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiry, signingCredentials: creds);
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse(user.UserId, user.Name, user.Email,
            user.Role.ToString(), tokenStr, newRefreshToken, expiry, user.EmployeeId, user.ProfileImageUrl);
    }
}
