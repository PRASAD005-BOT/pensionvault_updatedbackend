using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Members.Services.DTOs;
using Members.Domain.Entities;
using Members.Domain.Repositories;
using PensionVault.Shared.Contracts;
using PensionVault.Shared.Results;

namespace Members.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IEmployerRepository _employerRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IMemberRepository _memberRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;

    public AuthService(
        IUserRepository userRepo,
        IEmployerRepository employerRepo,
        INotificationRepository notificationRepo,
        IMemberRepository memberRepo,
        IUnitOfWork unitOfWork,
        IConfiguration config)
    {
        _userRepo = userRepo;
        _employerRepo = employerRepo;
        _notificationRepo = notificationRepo;
        _memberRepo = memberRepo;
        _unitOfWork = unitOfWork;
        _config = config;
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.FindByEmailAsync(request.Email);

        if (user == null)
        {
            var allEmployers = await _employerRepo.GetAllAsync();
            Employer? matchingEmployer = null;
            foreach (var emp in allEmployers)
            {
                if (string.IsNullOrEmpty(emp.ContactDetails)) continue;

                try
                {
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(emp.ContactDetails);
                    if (jsonDoc.RootElement.TryGetProperty("contactEmail", out var emailProp))
                    {
                        var emailVal = emailProp.GetString();
                        if (string.Equals(emailVal, request.Email, StringComparison.OrdinalIgnoreCase))
                        {
                            matchingEmployer = emp;
                            break;
                        }
                    }
                }
                catch { }

                if (emp.ContactDetails.Contains(request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    matchingEmployer = emp;
                    break;
                }
            }

            if (matchingEmployer != null)
            {
                bool isPassValid = !string.IsNullOrWhiteSpace(matchingEmployer.EmployerCode) && string.Equals(request.Password, matchingEmployer.EmployerCode, StringComparison.OrdinalIgnoreCase);

                if (isPassValid)
                {
                    user = new User
                    {
                        UserId = Guid.NewGuid(),
                        Name = matchingEmployer.CompanyName + " Rep",
                        Email = request.Email,
                        Role = UserRole.Employer,
                        OrganisationId = matchingEmployer.EmployerId,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                        Status = UserStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _userRepo.AddAsync(user);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }

        if (user == null)
            return ServiceResult<AuthResponse>.Fail("Invalid email or password.", 401);

        bool isValidPassword = false;
        try
        {
            if (!string.IsNullOrEmpty(user.PasswordHash) && BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                isValidPassword = true;
            }
        }
        catch { }

        if (!isValidPassword && user.Role == UserRole.Member)
        {
            var member = await _memberRepo.FindByUserIdAsync(user.UserId);
            if (member != null)
            {
                if (string.Equals(request.Password, member.MembershipNumber, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(request.Password, member.MemberId.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(request.Password, member.UserId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    isValidPassword = true;
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }

        if (!isValidPassword && user.Role == UserRole.Employer && user.OrganisationId.HasValue)
        {
            var employer = await _employerRepo.FindByIdAsync(user.OrganisationId.Value);
            if (employer != null)
            {
                if (!string.IsNullOrWhiteSpace(employer.EmployerCode) && string.Equals(request.Password, employer.EmployerCode, StringComparison.OrdinalIgnoreCase))
                {
                    isValidPassword = true;
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }

        if (!isValidPassword)
            return ServiceResult<AuthResponse>.Fail("Invalid email or password.", 401);

        if (!string.IsNullOrEmpty(request.Role) && !string.Equals(user.Role.ToString(), request.Role, StringComparison.OrdinalIgnoreCase))
            return ServiceResult<AuthResponse>.Fail($"This account is registered as '{user.Role}'. Please select the correct role to sign in.", 401);

        if (user.Status != UserStatus.Active)
            return ServiceResult<AuthResponse>.Fail("Account is not active.", 401);

        if (user.Role == UserRole.Employer && user.OrganisationId.HasValue)
        {
            var loginEmployer = await _employerRepo.FindByIdAsync(user.OrganisationId.Value);
            if (loginEmployer?.Status == EmployerStatus.Pending)
                return ServiceResult<AuthResponse>.Fail("Your company registration is pending admin approval.", 401);
            if (loginEmployer?.Status == EmployerStatus.Deregistered)
                return ServiceResult<AuthResponse>.Fail("Your company registration was rejected or deregistered. Please contact support.", 401);
        }

        return ServiceResult<AuthResponse>.Ok(await GenerateTokensAsync(user));
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepo.ExistsByEmailAsync(request.Email))
            return ServiceResult<AuthResponse>.Fail("Email already registered.", 409);

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return ServiceResult<AuthResponse>.Fail("Invalid role specified.", 400);

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
            var employerCode = EmployerService.GenerateEmployerCode();
            var newEmployer = new Employer
            {
                EmployerCode = employerCode,
                CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? request.Name + "'s Company" : request.CompanyName,
                RegistrationNumber = "PENDING-" + employerCode,
                ContactDetails = request.Email,
                // New self-registered companies await admin approval before they can sign in — see EmployersController Approve/Reject.
                Status = EmployerStatus.Pending
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

        return ServiceResult<AuthResponse>.Ok(await GenerateTokensAsync(user));
    }

    public async Task<ServiceResult<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepo.FindByRefreshTokenAsync(refreshToken);
        if (user is null)
            return ServiceResult<AuthResponse>.Fail("Invalid or expired refresh token.", 401);
        return ServiceResult<AuthResponse>.Ok(await GenerateTokensAsync(user));
    }

    private async Task<AuthResponse> GenerateTokensAsync(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? "PensionVault$SuperSecretKey#2024@JwtTokenSigningKey!MustBe32Chars";
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




