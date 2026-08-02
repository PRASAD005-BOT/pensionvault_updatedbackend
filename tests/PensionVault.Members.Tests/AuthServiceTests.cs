using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Members.Services;
using Members.Services.DTOs;
using Members.Domain.Entities;
using Members.Domain.Repositories;
using PensionVault.Shared.Contracts;

namespace PensionVault.Members.Tests;

/// <summary>
/// NUnit tests for AuthService — covering Login, Register, and Token Refresh flows.
/// All external dependencies (repositories, unit-of-work, configuration) are mocked
/// with Moq so no database connection is required.
/// </summary>
[TestFixture]
public class AuthServiceTests
{
    // ── Mocks ─────────────────────────────────────────────────────────────────
    private Mock<IUserRepository>         _userRepo        = null!;
    private Mock<IEmployerRepository>     _employerRepo    = null!;
    private Mock<INotificationRepository> _notifRepo       = null!;
    private Mock<IMemberRepository>       _memberRepo      = null!;
    private Mock<IUnitOfWork>             _unitOfWork      = null!;
    private Mock<IConfiguration>          _config          = null!;
    private Mock<IWebHostEnvironment>     _env             = null!;

    private AuthService _sut = null!;   // System Under Test

    // ── Shared test data ──────────────────────────────────────────────────────
    private static readonly Guid   TestUserId  = Guid.NewGuid();
    private const  string          TestEmail   = "john@example.com";
    private const  string          TestPwd     = "secure123";
    private const  string          TestName    = "John Doe";
    private const  string          JwtKey      = "PensionVault$SuperSecretKey#2024@JwtTokenSigningKey!";

    // ── Setup ─────────────────────────────────────────────────────────────────
    [SetUp]
    public void SetUp()
    {
        _userRepo     = new Mock<IUserRepository>();
        _employerRepo = new Mock<IEmployerRepository>();
        _notifRepo    = new Mock<INotificationRepository>();
        _memberRepo   = new Mock<IMemberRepository>();
        _unitOfWork   = new Mock<IUnitOfWork>();
        _config       = new Mock<IConfiguration>();
        _env          = new Mock<IWebHostEnvironment>();

        // Configure JWT settings returned by IConfiguration
        _config.Setup(c => c["Jwt:Key"]).Returns(JwtKey);
        _config.Setup(c => c["Jwt:Issuer"]).Returns("PensionVault");
        _config.Setup(c => c["Jwt:Audience"]).Returns("PensionVaultUsers");
        _config.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");

        // WebHostEnvironment: return a temp path so GetProfileImageUrl doesn't crash
        _env.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

        // UnitOfWork always succeeds
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _sut = new AuthService(
            _userRepo.Object,
            _employerRepo.Object,
            _notifRepo.Object,
            _memberRepo.Object,
            _unitOfWork.Object,
            _config.Object,
            _env.Object);
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private User MakeActiveUser(string password = TestPwd, UserRole role = UserRole.Member)
    {
        return new User
        {
            UserId       = TestUserId,
            Name         = TestName,
            Email        = TestEmail,
            Role         = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status       = UserStatus.Active
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LOGIN TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Auth.Login")]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var user = MakeActiveUser();
        _userRepo.Setup(r => r.FindByEmailAsync(TestEmail)).ReturnsAsync(user);

        var request = new LoginRequest(TestEmail, TestPwd, "Member");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.That(result.Success, Is.True, "Login with correct credentials should succeed");
        Assert.That(result.Value,      Is.Not.Null);
        Assert.That(result.Value!.Token, Is.Not.Empty, "A JWT token must be returned");
        Assert.That(result.Value.Email,  Is.EqualTo(TestEmail));
        Assert.That(result.Value.Role,   Is.EqualTo("Member"));
    }

    [Test]
    [Category("Auth.Login")]
    public async Task LoginAsync_WrongPassword_ReturnsFailure()
    {
        // Arrange
        var user = MakeActiveUser();
        _userRepo.Setup(r => r.FindByEmailAsync(TestEmail)).ReturnsAsync(user);
        _memberRepo.Setup(r => r.FindByUserIdAsync(TestUserId)).ReturnsAsync((Member?)null);

        var request = new LoginRequest(TestEmail, "wrongpassword", "Member");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(401));
        Assert.That(result.Error, Does.Contain("Invalid email or password"));
    }

    [Test]
    [Category("Auth.Login")]
    public async Task LoginAsync_EmailNotFound_ReturnsFailure()
    {
        // Arrange
        _userRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _employerRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Employer>());

        var request = new LoginRequest("nobody@example.com", "pass", "Member");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(401));
    }

    [Test]
    [Category("Auth.Login")]
    public async Task LoginAsync_WrongRoleSelected_ReturnsFailure()
    {
        // Arrange — user is a Member but tries to log in as FundAdmin
        var user = MakeActiveUser(role: UserRole.Member);
        _userRepo.Setup(r => r.FindByEmailAsync(TestEmail)).ReturnsAsync(user);

        var request = new LoginRequest(TestEmail, TestPwd, "FundAdmin");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(401));
        Assert.That(result.Error, Does.Contain("registered as"));
    }

    [Test]
    [Category("Auth.Login")]
    public async Task LoginAsync_InactiveUser_ReturnsFailure()
    {
        // Arrange
        var user = MakeActiveUser();
        user.Status = UserStatus.Inactive;
        _userRepo.Setup(r => r.FindByEmailAsync(TestEmail)).ReturnsAsync(user);

        var request = new LoginRequest(TestEmail, TestPwd, "Member");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(401));
        Assert.That(result.Error, Does.Contain("not active"));
    }

    [Test]
    [Category("Auth.Login")]
    public async Task LoginAsync_EmployerWithPendingStatus_ReturnsFailure()
    {
        // Arrange
        var empId = Guid.NewGuid();
        var user = MakeActiveUser(role: UserRole.Employer);
        user.OrganisationId = empId;

        var employer = new Employer
        {
            EmployerId   = empId,
            CompanyName  = "ACME Corp",
            Status       = EmployerStatus.Pending,
            EmployerCode = "EMP001",
            ContactEmail = TestEmail
        };

        _userRepo.Setup(r => r.FindByEmailAsync(TestEmail)).ReturnsAsync(user);
        _employerRepo.Setup(r => r.FindByIdAsync(empId)).ReturnsAsync(employer);

        var request = new LoginRequest(TestEmail, TestPwd, "Employer");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(401));
        Assert.That(result.Error, Does.Contain("pending admin approval"));
    }

    [Test]
    [Category("Auth.Login")]
    public async Task LoginAsync_FundAdminRole_ReturnsSuccessWithCorrectRole()
    {
        // Arrange
        var user = MakeActiveUser(role: UserRole.FundAdmin);
        _userRepo.Setup(r => r.FindByEmailAsync(TestEmail)).ReturnsAsync(user);

        var request = new LoginRequest(TestEmail, TestPwd, "FundAdmin");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Value!.Role, Is.EqualTo("FundAdmin"));
    }

    [Test]
    [Category("Auth.Login")]
    public async Task LoginAsync_NoRoleProvided_SkipsRoleCheck()
    {
        // Arrange — when Role is empty string, role check is skipped
        var user = MakeActiveUser(role: UserRole.Compliance);
        _userRepo.Setup(r => r.FindByEmailAsync(TestEmail)).ReturnsAsync(user);

        var request = new LoginRequest(TestEmail, TestPwd, "");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert — should succeed regardless of role value
        Assert.That(result.Success, Is.True);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REGISTER TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Auth.Register")]
    public async Task RegisterAsync_NewValidMember_ReturnsSuccessWithToken()
    {
        // Arrange
        _userRepo.Setup(r => r.ExistsByEmailAsync(TestEmail)).ReturnsAsync(false);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var request = new RegisterRequest(TestName, TestEmail, TestPwd, "Member", null, null, null);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Value,      Is.Not.Null);
        Assert.That(result.Value!.Token, Is.Not.Empty);
        Assert.That(result.Value.Email,  Is.EqualTo(TestEmail));
    }

    [Test]
    [Category("Auth.Register")]
    public async Task RegisterAsync_DuplicateEmail_ReturnsConflict()
    {
        // Arrange — email already registered
        _userRepo.Setup(r => r.ExistsByEmailAsync(TestEmail)).ReturnsAsync(true);

        var request = new RegisterRequest(TestName, TestEmail, TestPwd, "Member", null, null, null);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(409));
        Assert.That(result.Error, Does.Contain("already registered"));
    }

    [Test]
    [Category("Auth.Register")]
    public async Task RegisterAsync_EmployerRole_ReturnsBadRequest()
    {
        // Arrange — employer self-registration is disabled
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);

        var request = new RegisterRequest("ACME Corp", "hr@acme.com", TestPwd, "Employer", null, null, null);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error, Does.Contain("disabled"));
    }

    [Test]
    [Category("Auth.Register")]
    public async Task RegisterAsync_InvalidRoleString_ReturnsBadRequest()
    {
        // Arrange
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);

        var request = new RegisterRequest(TestName, TestEmail, TestPwd, "SuperAdmin", null, null, null);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error, Does.Contain("Invalid role"));
    }

    [Test]
    [Category("Auth.Register")]
    public async Task RegisterAsync_PasswordIsHashed_NotStoredInPlainText()
    {
        // Arrange
        User? capturedUser = null;
        _userRepo.Setup(r => r.ExistsByEmailAsync(TestEmail)).ReturnsAsync(false);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                 .Callback<User>(u => capturedUser = u)
                 .Returns(Task.CompletedTask);

        var request = new RegisterRequest(TestName, TestEmail, TestPwd, "Member", null, null, null);

        // Act
        await _sut.RegisterAsync(request);

        // Assert — stored hash must NOT equal the plain-text password
        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser!.PasswordHash, Is.Not.EqualTo(TestPwd));
        Assert.That(BCrypt.Net.BCrypt.Verify(TestPwd, capturedUser.PasswordHash), Is.True,
            "Stored password hash must be a valid BCrypt hash of the plain-text password");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REFRESH TOKEN TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Auth.Refresh")]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewToken()
    {
        // Arrange
        const string refreshToken = "valid-refresh-token-abc";
        var user = MakeActiveUser();
        user.RefreshToken       = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        _userRepo.Setup(r => r.FindByRefreshTokenAsync(refreshToken)).ReturnsAsync(user);

        // Act
        var result = await _sut.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Value!.Token, Is.Not.Empty);
    }

    [Test]
    [Category("Auth.Refresh")]
    public async Task RefreshTokenAsync_InvalidToken_ReturnsFailure()
    {
        // Arrange
        _userRepo.Setup(r => r.FindByRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.RefreshTokenAsync("bad-token");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(401));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // JWT CONTENT TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Auth.JWT")]
    public async Task LoginAsync_SuccessfulLogin_TokenContainsCorrectClaims()
    {
        // Arrange
        var user = MakeActiveUser();
        _userRepo.Setup(r => r.FindByEmailAsync(TestEmail)).ReturnsAsync(user);

        var request = new LoginRequest(TestEmail, TestPwd, "Member");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert — decode token and verify claims
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(result.Value!.Token);

        Assert.That(jwt.Subject, Is.EqualTo(TestUserId.ToString()),      "sub claim must equal UserId");
        Assert.That(jwt.Claims.First(c => c.Type == "email").Value,
                    Is.EqualTo(TestEmail),                                "email claim must match");
    }
}
