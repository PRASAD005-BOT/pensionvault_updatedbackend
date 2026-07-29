using System.IO;
using Microsoft.AspNetCore.Hosting;
using Members.Services.DTOs;
using Members.Domain.Entities;
using Members.Domain.Repositories;
using PensionVault.Shared.Contracts;

namespace Members.Services;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepo;
    private readonly IEmployerRepository _employerRepo;
    private readonly IFundAccountRepository _accountRepo;
    private readonly IFundSchemeRepository _schemeRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IContributionRepository _contributionRepo;
    private readonly ILedgerRepository _ledgerRepo;
    private readonly IClaimRepository _claimRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;

    public MemberService(
        IMemberRepository memberRepo,
        IEmployerRepository employerRepo,
        IFundAccountRepository accountRepo,
        IFundSchemeRepository schemeRepo,
        IUserRepository userRepo,
        INotificationRepository notificationRepo,
        IContributionRepository contributionRepo,
        ILedgerRepository ledgerRepo,
        IClaimRepository claimRepo,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment env)
    {
        _memberRepo = memberRepo;
        _employerRepo = employerRepo;
        _accountRepo = accountRepo;
        _schemeRepo = schemeRepo;
        _userRepo = userRepo;
        _notificationRepo = notificationRepo;
        _contributionRepo = contributionRepo;
        _ledgerRepo = ledgerRepo;
        _claimRepo = claimRepo;
        _unitOfWork = unitOfWork;
        _env = env;
    }

    public async Task<IEnumerable<MemberResponse>> GetAllAsync(Guid? employerId = null)
    {
        var members = await _memberRepo.GetAllAsync(employerId);
        return members.Select(ToResponse);
    }

    public async Task<MemberResponse> GetByIdAsync(Guid id)
    {
        var member = await _memberRepo.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"Member {id} not found.");
        return ToResponse(member);
    }

    // Returns null when the user has no member profile (an expected case for
    // non-member roles). Callers decide the HTTP outcome (typically 404).
    public async Task<MemberResponse?> GetByUserIdAsync(Guid userId)
    {
        var member = await _memberRepo.FindByUserIdAsync(userId);
        return member is null ? null : ToResponse(member);
    }

    public async Task<MemberResponse> CreateAsync(CreateMemberRequest request)
    {
        var today = DateTime.UtcNow.Date;
        if (request.DateOfBirth.Date > today)
            throw new ArgumentException("Date of birth cannot exceed the current date.");

        if (request.JoiningDate.Date > today)
            throw new ArgumentException("Joining date cannot exceed the current date and year.");

        if (await _memberRepo.ExistsByMembershipNumberAsync(request.MembershipNumber))
            throw new InvalidOperationException("Membership number already exists.");

        var existingUser = await _userRepo.FindByEmailAsync(request.Email)
            ?? await _userRepo.FindByIdAsync(request.UserId);

        Guid targetUserId;
        if (existingUser == null)
        {
            targetUserId = request.UserId == Guid.Empty ? Guid.NewGuid() : request.UserId;
            var placeholderUser = new User
            {
                UserId = targetUserId,
                Name = request.Name,
                Email = request.Email,
                Role = UserRole.Member,
                PasswordHash = "",
                OrganisationId = request.EmployerId,
                EmployerCode = request.MembershipNumber,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepo.AddAsync(placeholderUser);
        }
        else
        {
            targetUserId = existingUser.UserId;
            existingUser.Name = request.Name;
            existingUser.OrganisationId = request.EmployerId;
            existingUser.EmployerCode = request.MembershipNumber;
        }

        var member = new Member
        {
            UserId = targetUserId,
            MembershipNumber = request.MembershipNumber,
            Name = request.Name,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            NationalIdRef = request.NationalIdRef,
            EmployerId = request.EmployerId,
            JoiningDate = request.JoiningDate,
            DateOfRetirement = request.DateOfRetirement ?? request.DateOfBirth.AddYears(60),
            NomineeName = request.NomineeName,
            NomineeRelation = request.NomineeRelation,
            NomineeBankAccount = request.NomineeBankAccount,
            NomineePercent = request.NomineePercent ?? 100,
            Status = MemberStatus.Active
        };
        await _memberRepo.AddAsync(member);

        var employer = await _employerRepo.FindByIdAsync(request.EmployerId);
        if (employer != null) employer.EnrolledMemberCount++;

        var defaultScheme = await _schemeRepo.GetFirstAsync();
        if (defaultScheme != null)
        {
            await _accountRepo.AddAsync(new ExternalFundAccount(
                AccountId: Guid.NewGuid(),
                MemberId: member.MemberId,
                SchemeId: defaultScheme.SchemeId,
                AccountOpenDate: DateTime.UtcNow,
                EmployeeContributionBalance: 0,
                EmployerContributionBalance: 0,
                PensionBalance: 0,
                InterestAccrued: 0,
                TotalBalance: 0,
                VestingPercent: 100,
                Status: "Active"
            ));
        }

        await _notificationRepo.AddAsync(new Notification
        {
            UserId = member.UserId,
            Message = $"Welcome to PensionVault, {member.Name}! Your EPF account has been created successfully.",
            Category = NotificationCategory.Contribution,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });

        var admins = await GetAdminUsersAsync();
        var adminNotifications = admins.Select(adminUser => new Notification
        {
            UserId = adminUser.UserId,
            Message = $"New member enrolled: {member.Name}. Membership Number: {member.MembershipNumber}.",
            Category = NotificationCategory.Compliance,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });
        await _notificationRepo.AddRangeAsync(adminNotifications);

        await _unitOfWork.SaveChangesAsync();
        return await GetByIdAsync(member.MemberId);
    }

    public async Task<MemberResponse> UpdateAsync(Guid id, UpdateMemberRequest request)
    {
        var today = DateTime.UtcNow.Date;
        if (request.DateOfBirth.Date > today)
            throw new ArgumentException("Date of birth cannot exceed the current date.");

        if (request.JoiningDate.Date > today)
            throw new ArgumentException("Joining date cannot exceed the current date and year.");

        var member = await _memberRepo.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"Member {id} not found.");

        member.Name = request.Name;
        member.DateOfBirth = request.DateOfBirth;
        member.Gender = request.Gender;
        member.NationalIdRef = request.NationalIdRef;
        member.DateOfRetirement = request.DateOfRetirement;
        member.NomineeName = request.NomineeName;
        member.NomineeRelation = request.NomineeRelation;
        member.NomineeBankAccount = request.NomineeBankAccount;
        member.NomineePercent = request.NomineePercent ?? 100;
        member.Status = request.Status;

        if (member.EmployerId != request.EmployerId)
        {
            var oldEmp = await _employerRepo.FindByIdAsync(member.EmployerId);
            if (oldEmp != null) oldEmp.EnrolledMemberCount--;
            member.EmployerId = request.EmployerId;
            var newEmp = await _employerRepo.FindByIdAsync(request.EmployerId);
            if (newEmp != null) newEmp.EnrolledMemberCount++;
        }

        member.JoiningDate = request.JoiningDate;

        // Update linked user email & phone
        if (member.User != null)
        {
            member.User.Email = request.Email;
            if (!string.IsNullOrWhiteSpace(request.Phone))
                member.User.Phone = request.Phone;
            member.User.Name = request.Name;
            member.User.OrganisationId = request.EmployerId;
            member.User.EmployerCode = member.MembershipNumber;
        }

        await _unitOfWork.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<MemberResponse> SelfEnrollAsync(Guid userId, SelfEnrollMemberRequest request)
    {
        var today = DateTime.UtcNow.Date;
        if (request.DateOfBirth.Date > today)
            throw new ArgumentException("Date of birth cannot exceed the current date.");

        if (await _memberRepo.ExistsByUserIdAsync(userId))
            throw new InvalidOperationException("You have already submitted an enrollment profile.");

        var user = await _userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var member = new Member
        {
            UserId = userId,
            MembershipNumber = $"PENDING-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            Name = user.Name,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            NationalIdRef = request.NationalIdRef,
            EmployerId = request.EmployerId,
            JoiningDate = DateTime.UtcNow,
            DateOfRetirement = request.DateOfBirth.AddYears(60),
            NomineeName = request.NomineeName,
            NomineeRelation = request.NomineeRelation,
            NomineeBankAccount = request.NomineeBankAccount,
            NomineePercent = request.NomineePercent ?? 100,
            Status = MemberStatus.Active
        };
        await _memberRepo.AddAsync(member);

        user.OrganisationId = request.EmployerId;
        user.EmployerCode = member.MembershipNumber;

        var employer = await _employerRepo.FindByIdAsync(request.EmployerId);
        if (employer != null) employer.EnrolledMemberCount++;

        var defaultScheme = await _schemeRepo.GetFirstAsync();
        if (defaultScheme != null)
        {
            await _accountRepo.AddAsync(new ExternalFundAccount(
                AccountId: Guid.NewGuid(),
                MemberId: member.MemberId,
                SchemeId: defaultScheme.SchemeId,
                AccountOpenDate: DateTime.UtcNow,
                EmployeeContributionBalance: 0,
                EmployerContributionBalance: 0,
                PensionBalance: 0,
                InterestAccrued: 0,
                TotalBalance: 0,
                VestingPercent: 100,
                Status: "Active"
            ));
        }

        var admins = await GetAdminUsersAsync();
        var adminNotifications = admins.Select(adminUser => new Notification
        {
            UserId = adminUser.UserId,
            Message = $"Employee {user.Name} has submitted their profile. Awaiting Membership Number assignment.",
            Category = NotificationCategory.Compliance,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });
        await _notificationRepo.AddRangeAsync(adminNotifications);

        await _unitOfWork.SaveChangesAsync();
        return await GetByIdAsync(member.MemberId);
    }

    public async Task<MemberResponse> ApproveAsync(Guid id, ApproveMemberRequest request)
    {
        var member = await _memberRepo.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Member not found.");

        if (await _memberRepo.ExistsByMembershipNumberAsync(request.MembershipNumber, id))
            throw new InvalidOperationException("Membership number already exists.");

        member.MembershipNumber = request.MembershipNumber;

        var linkedUser = await _userRepo.FindByIdAsync(member.UserId);
        if (linkedUser != null)
        {
            linkedUser.OrganisationId = member.EmployerId;
            linkedUser.EmployerCode = member.MembershipNumber;
        }

        if (member.EmployerId != request.EmployerId)
        {
            var oldEmp = await _employerRepo.FindByIdAsync(member.EmployerId);
            if (oldEmp != null) oldEmp.EnrolledMemberCount--;
            member.EmployerId = request.EmployerId;
            var newEmp = await _employerRepo.FindByIdAsync(request.EmployerId);
            if (newEmp != null) newEmp.EnrolledMemberCount++;
        }

        await _notificationRepo.AddAsync(new Notification
        {
            UserId = member.UserId,
            Message = $"Your profile has been approved! Your Membership Number is {member.MembershipNumber}.",
            Category = NotificationCategory.Compliance,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<IEnumerable<object>> GetFundAccountsAsync(Guid memberId)
    {
        var accounts = await _accountRepo.GetByMemberAsync(memberId);
        var resultList = new List<object>();
        foreach (var a in accounts)
        {
            var scheme = await _schemeRepo.FindByIdAsync(a.SchemeId);
            resultList.Add(new
            {
                a.AccountId,
                a.MemberId,
                a.SchemeId,
                SchemeName = scheme?.SchemeName ?? "Employee Provident Fund",
                a.AccountOpenDate,
                a.EmployeeContributionBalance,
                a.EmployerContributionBalance,
                a.PensionBalance,
                a.InterestAccrued,
                a.TotalBalance,
                a.VestingPercent,
                Status = a.Status.ToString(),
                EmployeeContribution = a.EmployeeContributionBalance,
                EmployerContribution = a.EmployerContributionBalance,
                InterestEarned = a.InterestAccrued
            });
        }
        return resultList;
    }

    public async Task<IEnumerable<object>> GetContributionsAsync(Guid memberId)
    {
        var contributions = await _contributionRepo.GetByMemberAsync(memberId);
        return contributions.Select(c => (object)new
        {
            c.ContributionId,
            c.Period,
            c.EmployeeAmount,
            c.EmployerAmount,
            c.TotalAmount,
            c.PostedDate,
            Status = c.Status.ToString()
        });
    }

    public async Task<IEnumerable<object>> GetLedgerAsync(Guid memberId)
    {
        var accounts = await _accountRepo.GetByMemberAsync(memberId);
        var accountIds = accounts.Select(a => a.AccountId).ToList();

        var allEntries = new List<object>();
        foreach (var accountId in accountIds)
        {
            var entries = await _ledgerRepo.GetByAccountAsync(accountId);
            allEntries.AddRange(entries.Select(e => (object)new
            {
                e.EntryId,
                e.AccountId,
                EntryType = e.EntryType.ToString(),
                e.Amount,
                e.BalanceAfter,
                e.EntryDate,
                e.ReferenceId,
                Status = e.Status.ToString()
            }));
        }
        return allEntries.OrderByDescending(e => ((dynamic)e).EntryDate);
    }

    public async Task<IEnumerable<object>> GetClaimsAsync(Guid memberId)
    {
        var claims = await _claimRepo.GetAllAsync();
        return claims
            .Where(c => c.MemberId == memberId)
            .Select(c => (object)new
            {
                c.ClaimId,
                ClaimType = c.ClaimType.ToString(),
                c.ClaimDate,
                c.EligibleAmount,
                Status = c.Status.ToString()
            });
    }

    private string? GetProfileImageUrl(Guid userId)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "profiles");
        if (!Directory.Exists(folder)) return null;

        var files = Directory.GetFiles(folder, $"{userId}.*");
        if (files.Length > 0)
        {
            return $"/uploads/profiles/{Path.GetFileName(files[0])}";
        }
        return null;
    }

    private MemberResponse ToResponse(Member m) => new(
        m.MemberId, m.MembershipNumber, m.Name, m.DateOfBirth,
        m.Gender, m.NationalIdRef, m.EmployerId,
        m.Employer?.CompanyName ?? "", m.JoiningDate,
        m.DateOfRetirement, m.NomineeName, m.NomineeRelation, m.NomineeBankAccount, m.NomineePercent, m.Status.ToString(), GetProfileImageUrl(m.UserId),
        m.User?.Email ?? "", m.UserId, m.User?.Phone);

    private async Task<List<User>> GetAdminUsersAsync()
    {
        var admins = await _userRepo.GetByRoleAsync(UserRole.Admin);
        var fundAdmins = await _userRepo.GetByRoleAsync(UserRole.FundAdmin);
        var compliance = await _userRepo.GetByRoleAsync(UserRole.Compliance);

        var all = new List<User>();
        all.AddRange(admins);
        all.AddRange(fundAdmins);
        all.AddRange(compliance);
        return all.GroupBy(u => u.UserId).Select(g => g.First()).ToList();
    }
}