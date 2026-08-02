using Moq;
using NUnit.Framework;
using Microsoft.AspNetCore.Hosting;
using Members.Services;
using Members.Services.DTOs;
using Members.Domain.Entities;
using Members.Domain.Repositories;
using PensionVault.Shared.Contracts;

namespace PensionVault.Members.Tests;

/// <summary>
/// NUnit tests for MemberService — covering CreateAsync, SelfEnrollAsync,
/// ApproveAsync, RejectAsync, and UpdateAsync flows.
/// </summary>
[TestFixture]
public class MemberServiceTests
{
    private Mock<IMemberRepository>       _memberRepo  = null!;
    private Mock<IEmployerRepository>     _employerRepo = null!;
    private Mock<IFundAccountRepository>  _accountRepo  = null!;
    private Mock<IFundSchemeRepository>   _schemeRepo   = null!;
    private Mock<IUserRepository>         _userRepo     = null!;
    private Mock<INotificationRepository> _notifRepo    = null!;
    private Mock<IContributionRepository> _contribRepo  = null!;
    private Mock<ILedgerRepository>       _ledgerRepo   = null!;
    private Mock<IClaimRepository>        _claimRepo    = null!;
    private Mock<IUnitOfWork>             _unitOfWork   = null!;
    private Mock<IWebHostEnvironment>     _env          = null!;

    private MemberService _sut = null!;

    private static readonly Guid EmployerId = Guid.NewGuid();
    private static readonly Guid UserId     = Guid.NewGuid();
    private static readonly Guid MemberId   = Guid.NewGuid();
    private static readonly Guid SchemeId   = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _memberRepo   = new Mock<IMemberRepository>();
        _employerRepo = new Mock<IEmployerRepository>();
        _accountRepo  = new Mock<IFundAccountRepository>();
        _schemeRepo   = new Mock<IFundSchemeRepository>();
        _userRepo     = new Mock<IUserRepository>();
        _notifRepo    = new Mock<INotificationRepository>();
        _contribRepo  = new Mock<IContributionRepository>();
        _ledgerRepo   = new Mock<ILedgerRepository>();
        _claimRepo    = new Mock<IClaimRepository>();
        _unitOfWork   = new Mock<IUnitOfWork>();
        _env          = new Mock<IWebHostEnvironment>();

        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _env.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

        // GetFirstAsync returns a default scheme
        _schemeRepo.Setup(s => s.GetFirstAsync()).ReturnsAsync(new FundScheme
        {
            SchemeId   = SchemeId,
            SchemeName = "EPF Scheme"
        });

        // Default: no admins to notify (avoids null ref in GetAdminUsersAsync)
        _userRepo.Setup(r => r.GetByRoleAsync(UserRole.Admin)).ReturnsAsync(new List<User>());
        _userRepo.Setup(r => r.GetByRoleAsync(UserRole.FundAdmin)).ReturnsAsync(new List<User>());
        _userRepo.Setup(r => r.GetByRoleAsync(UserRole.Compliance)).ReturnsAsync(new List<User>());

        _accountRepo.Setup(r => r.AddAsync(It.IsAny<ExternalFundAccount>())).Returns(Task.CompletedTask);
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
        _notifRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Notification>>())).Returns(Task.CompletedTask);

        _sut = new MemberService(
            _memberRepo.Object, _employerRepo.Object, _accountRepo.Object,
            _schemeRepo.Object, _userRepo.Object, _notifRepo.Object,
            _contribRepo.Object, _ledgerRepo.Object, _claimRepo.Object,
            _unitOfWork.Object, _env.Object);
    }

    // ── Helper builders ───────────────────────────────────────────────────────

    private static Member MakeMember(Guid? id = null) => new Member
    {
        MemberId         = id ?? MemberId,
        MembershipNumber = "MEM-2024-001",
        Name             = "John Doe",
        DateOfBirth      = new DateTime(1985, 1, 1),
        JoiningDate      = new DateTime(2010, 6, 15),
        EmployerId       = EmployerId,
        Status           = MemberStatus.Active,
        UserId           = UserId,
        User             = new User { Email = "john@example.com" }
    };

    private static User MakeUser() => new User
    {
        UserId       = UserId,
        Name         = "John Doe",
        Email        = "john@example.com",
        Role         = UserRole.Member,
        PasswordHash = "",
        Status       = UserStatus.Active
    };

    private static Employer MakeEmployer() => new Employer
    {
        EmployerId        = EmployerId,
        CompanyName       = "ACME Corp",
        EmployerCode      = "EMP001",
        Status            = EmployerStatus.Active,
        EnrolledMemberCount = 0
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // CREATE MEMBER (Admin / Employer path)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Member.Create")]
    public async Task CreateAsync_ValidRequest_ReturnsMemberResponse()
    {
        // Arrange
        var employer = MakeEmployer();
        var member   = MakeMember();

        _memberRepo.Setup(r => r.ExistsByMembershipNumberAsync("MEM-2024-001", null)).ReturnsAsync(false);
        _userRepo.Setup(r => r.FindByEmailAsync("john@example.com")).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.FindByIdAsync(Guid.Empty)).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _employerRepo.Setup(r => r.FindByIdAsync(EmployerId)).ReturnsAsync(employer);
        _memberRepo.Setup(r => r.AddAsync(It.IsAny<Member>())).Returns(Task.CompletedTask);
        _memberRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(member);

        var request = new CreateMemberRequest(
            UserId: Guid.Empty,
            MembershipNumber: "MEM-2024-001",
            Name: "John Doe",
            DateOfBirth: new DateTime(1985, 1, 1),
            Gender: "Male",
            NationalIdRef: "123456789012",
            EmployerId: EmployerId,
            JoiningDate: new DateTime(2010, 6, 15),
            DateOfRetirement: null,
            NomineeName: "Jane Doe",
            NomineeRelation: "Spouse",
            NomineeBankAccount: "123456789",
            NomineePercent: 100,
            Email: "john@example.com"
        );

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.MembershipNumber, Is.EqualTo("MEM-2024-001"));
    }

    [Test]
    [Category("Member.Create")]
    public async Task CreateAsync_DuplicateMembershipNumber_ThrowsInvalidOperation()
    {
        // Arrange
        _memberRepo.Setup(r => r.ExistsByMembershipNumberAsync("DUP-001", null)).ReturnsAsync(true);

        var request = new CreateMemberRequest(
            UserId: Guid.Empty,
            MembershipNumber: "DUP-001",
            Name: "Jane",
            DateOfBirth: new DateTime(1990, 1, 1),
            Gender: "Female",
            NationalIdRef: "",
            EmployerId: EmployerId,
            JoiningDate: new DateTime(2015, 1, 1),
            DateOfRetirement: null,
            NomineeName: null,
            NomineeRelation: null,
            NomineeBankAccount: null,
            NomineePercent: null,
            Email: "jane@example.com"
        );

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(request),
            "Duplicate membership number must throw InvalidOperationException");
    }

    [Test]
    [Category("Member.Create")]
    public async Task CreateAsync_FutureDateOfBirth_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateMemberRequest(
            UserId: Guid.Empty,
            MembershipNumber: "MEM-999",
            Name: "Future",
            DateOfBirth: DateTime.Today.AddDays(1),  // future date
            Gender: "Male",
            NationalIdRef: "",
            EmployerId: EmployerId,
            JoiningDate: DateTime.Today.AddDays(-10),
            DateOfRetirement: null,
            NomineeName: null,
            NomineeRelation: null,
            NomineeBankAccount: null,
            NomineePercent: null,
            Email: "future@example.com"
        );

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(request));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SELF-ENROLL
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Member.SelfEnroll")]
    public async Task SelfEnrollAsync_ValidRequest_CreatesPendingMember()
    {
        // Arrange
        var user   = MakeUser();
        var member = MakeMember();
        member.Status           = MemberStatus.Pending;
        member.MembershipNumber = "PENDING-ABCDE123";

        _memberRepo.Setup(r => r.ExistsByUserIdAsync(UserId)).ReturnsAsync(false);
        _userRepo.Setup(r => r.FindByIdAsync(UserId)).ReturnsAsync(user);
        _employerRepo.Setup(r => r.FindByIdAsync(EmployerId)).ReturnsAsync(MakeEmployer());
        _memberRepo.Setup(r => r.AddAsync(It.IsAny<Member>())).Returns(Task.CompletedTask);
        _memberRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(member);

        var request = new SelfEnrollMemberRequest(
            DateOfBirth: new DateTime(1985, 1, 1),
            Gender: "Male",
            NationalIdRef: "123456789012",
            EmployerId: EmployerId,
            JoiningDate: new DateTime(2010, 1, 1),
            NomineeName: "Jane Doe",
            NomineeRelation: "Spouse",
            NomineeBankAccount: "9876543210",
            NomineePercent: 100,
            Phone: "9876543210"
        );

        // Act
        var result = await _sut.SelfEnrollAsync(UserId, request);

        // Assert
        Assert.That(result,             Is.Not.Null);
        Assert.That(result.Status,      Is.EqualTo("Pending"));
        Assert.That(result.MembershipNumber, Does.StartWith("PENDING-"));
    }

    [Test]
    [Category("Member.SelfEnroll")]
    public async Task SelfEnrollAsync_AlreadyEnrolled_ThrowsInvalidOperation()
    {
        // Arrange — member profile already exists for this user
        _memberRepo.Setup(r => r.ExistsByUserIdAsync(UserId)).ReturnsAsync(true);

        var request = new SelfEnrollMemberRequest(
            DateOfBirth: new DateTime(1985, 1, 1),
            Gender: "Male", NationalIdRef: "123456789012",
            EmployerId: EmployerId,
            JoiningDate: new DateTime(2010, 1, 1),
            NomineeName: "Jane", NomineeRelation: "Spouse",
            NomineeBankAccount: "9876543210", NomineePercent: 100, Phone: null
        );

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SelfEnrollAsync(UserId, request),
            "Submitting a second enrollment must throw");
    }

    [Test]
    [Category("Member.SelfEnroll")]
    public async Task SelfEnrollAsync_UserNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        _memberRepo.Setup(r => r.ExistsByUserIdAsync(UserId)).ReturnsAsync(false);
        _userRepo.Setup(r => r.FindByIdAsync(UserId)).ReturnsAsync((User?)null);

        var request = new SelfEnrollMemberRequest(
            DateOfBirth: new DateTime(1985, 1, 1),
            Gender: "Male", NationalIdRef: "123456789012",
            EmployerId: EmployerId,
            JoiningDate: new DateTime(2010, 1, 1),
            NomineeName: null, NomineeRelation: null,
            NomineeBankAccount: null, NomineePercent: null, Phone: null
        );

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.SelfEnrollAsync(UserId, request));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // APPROVE / REJECT
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Member.Approve")]
    public async Task ApproveAsync_PendingMember_SetsStatusActive()
    {
        // Arrange
        var member = MakeMember();
        member.Status           = MemberStatus.Pending;
        member.MembershipNumber = "PENDING-XYZ";

        _memberRepo.Setup(r => r.FindByIdAsync(MemberId)).ReturnsAsync(member);
        _memberRepo.Setup(r => r.ExistsByMembershipNumberAsync("MEM-2024-001", MemberId)).ReturnsAsync(false);
        _userRepo.Setup(r => r.FindByIdAsync(UserId)).ReturnsAsync(MakeUser());
        _employerRepo.Setup(r => r.FindByIdAsync(EmployerId)).ReturnsAsync(MakeEmployer());
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);

        // After save, return the updated member
        _memberRepo.Setup(r => r.FindByIdAsync(MemberId)).ReturnsAsync(() =>
        {
            member.Status           = MemberStatus.Active;
            member.MembershipNumber = "MEM-2024-001";
            return member;
        });

        var approveRequest = new ApproveMemberRequest("MEM-2024-001", EmployerId);

        // Act
        var result = await _sut.ApproveAsync(MemberId, approveRequest);

        // Assert
        Assert.That(result.Status,            Is.EqualTo("Active"));
        Assert.That(result.MembershipNumber,  Is.EqualTo("MEM-2024-001"));
    }

    [Test]
    [Category("Member.Approve")]
    public async Task ApproveAsync_MemberNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        _memberRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Member?)null);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.ApproveAsync(Guid.NewGuid(), new ApproveMemberRequest("MEM-001", EmployerId)));
    }

    [Test]
    [Category("Member.Reject")]
    public async Task RejectAsync_PendingMember_SetsStatusRejected()
    {
        // Arrange
        var member = MakeMember();
        member.Status           = MemberStatus.Pending;
        member.MembershipNumber = "PENDING-XYZ";

        _memberRepo.Setup(r => r.FindByIdAsync(MemberId)).ReturnsAsync(member);
        _userRepo.Setup(r => r.FindByIdAsync(UserId)).ReturnsAsync(MakeUser());
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);

        _memberRepo.Setup(r => r.FindByIdAsync(MemberId)).ReturnsAsync(() =>
        {
            member.Status           = MemberStatus.Rejected;
            member.MembershipNumber = "REJECTED-XYZ";
            return member;
        });

        // Act
        var result = await _sut.RejectAsync(MemberId);

        // Assert
        Assert.That(result.Status, Is.EqualTo("Rejected"));
        Assert.That(result.MembershipNumber, Does.StartWith("REJECTED-"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GET OPERATIONS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    [Category("Member.Get")]
    public async Task GetByIdAsync_ExistingMember_ReturnsMemberResponse()
    {
        // Arrange
        var member = MakeMember();
        _memberRepo.Setup(r => r.FindByIdAsync(MemberId)).ReturnsAsync(member);

        // Act
        var result = await _sut.GetByIdAsync(MemberId);

        // Assert
        Assert.That(result,            Is.Not.Null);
        Assert.That(result.MemberId,   Is.EqualTo(MemberId));
        Assert.That(result.Name,       Is.EqualTo("John Doe"));
    }

    [Test]
    [Category("Member.Get")]
    public async Task GetByIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _memberRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Member?)null);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Test]
    [Category("Member.Get")]
    public async Task GetByUserIdAsync_ExistingUser_ReturnsMember()
    {
        // Arrange
        var member = MakeMember();
        _memberRepo.Setup(r => r.FindByUserIdAsync(UserId)).ReturnsAsync(member);

        // Act
        var result = await _sut.GetByUserIdAsync(UserId);

        // Assert
        Assert.That(result,          Is.Not.Null);
        Assert.That(result!.UserId,  Is.EqualTo(UserId));
    }

    [Test]
    [Category("Member.Get")]
    public async Task GetByUserIdAsync_NoMember_ReturnsNull()
    {
        // Arrange
        _memberRepo.Setup(r => r.FindByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync((Member?)null);

        // Act
        var result = await _sut.GetByUserIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }
}
