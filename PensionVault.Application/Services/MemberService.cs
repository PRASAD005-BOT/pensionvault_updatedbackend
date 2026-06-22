using PensionVault.Application.DTOs.Members;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Application.Services;

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
        IUnitOfWork unitOfWork)
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

    public async Task<MemberResponse> GetByUserIdAsync(Guid userId)
    {
        var member = await _memberRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Member profile not found for the current user.");
        return ToResponse(member);
    }

    public async Task<MemberResponse> CreateAsync(CreateMemberRequest request)
    {
        if (await _memberRepo.ExistsByMembershipNumberAsync(request.MembershipNumber))
            throw new InvalidOperationException("Membership number already exists.");

        var member = new Member
        {
            UserId = request.UserId,
            MembershipNumber = request.MembershipNumber,
            Name = request.Name,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            NationalIdRef = request.NationalIdRef,
            EmployerId = request.EmployerId,
            JoiningDate = request.JoiningDate,
            DateOfRetirement = request.DateOfRetirement ?? request.DateOfBirth.AddYears(60),
            NomineeDetails = request.NomineeDetails,
            Status = MemberStatus.Active
        };
        await _memberRepo.AddAsync(member);

        var employer = await _employerRepo.FindByIdAsync(request.EmployerId);
        if (employer != null) employer.EnrolledMemberCount++;

        var defaultScheme = await _schemeRepo.GetFirstAsync();
        if (defaultScheme != null)
        {
            await _accountRepo.AddAsync(new FundAccount
            {
                MemberId = member.MemberId,
                SchemeId = defaultScheme.SchemeId,
                AccountOpenDate = DateTime.UtcNow,
                VestingPercent = 100,
                Status = FundAccountStatus.Active
            });
        }

        await _notificationRepo.AddAsync(new Notification
        {
            UserId = member.UserId,
            Message = $"Welcome to PensionVault, {member.Name}! Your EPF account has been created successfully.",
            Category = NotificationCategory.Contribution,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();
        return await GetByIdAsync(member.MemberId);
    }

    public async Task<MemberResponse> UpdateAsync(Guid id, UpdateMemberRequest request)
    {
        var member = await _memberRepo.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"Member {id} not found.");

        member.Name = request.Name;
        member.Gender = request.Gender;
        member.NationalIdRef = request.NationalIdRef;
        member.DateOfRetirement = request.DateOfRetirement;
        member.NomineeDetails = request.NomineeDetails;
        member.Status = request.Status;

        await _unitOfWork.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<MemberResponse> SelfEnrollAsync(Guid userId, SelfEnrollMemberRequest request)
    {
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
            NomineeDetails = request.NomineeDetails,
            Status = MemberStatus.Active
        };
        await _memberRepo.AddAsync(member);

        var employer = await _employerRepo.FindByIdAsync(request.EmployerId);
        if (employer != null) employer.EnrolledMemberCount++;

        var defaultScheme = await _schemeRepo.GetFirstAsync();
        if (defaultScheme != null)
        {
            await _accountRepo.AddAsync(new FundAccount
            {
                MemberId = member.MemberId,
                SchemeId = defaultScheme.SchemeId,
                AccountOpenDate = DateTime.UtcNow,
                VestingPercent = 100,
                Status = FundAccountStatus.Active
            });
        }

        var adminUserIds = await _userRepo.GetByRoleAsync(UserRole.Admin);
        var adminNotifications = adminUserIds.Select(adminUser => new Notification
        {
            UserId = adminUser.UserId,
            Message = $"Employee {user.Name} has submitted their profile. Awaiting Membership Number assignment.",
            Category = NotificationCategory.Compliance,
            Status = NotificationStatus.Unread
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
        return accounts.Select(a => (object)new
        {
            a.AccountId, a.MemberId, a.SchemeId,
            SchemeName = a.Scheme.SchemeName,
            a.AccountOpenDate, a.EmployeeContributionBalance,
            a.EmployerContributionBalance, a.InterestAccrued,
            a.TotalBalance, a.VestingPercent, Status = a.Status.ToString()
        });
    }

    public async Task<IEnumerable<object>> GetContributionsAsync(Guid memberId)
    {
        var contributions = await _contributionRepo.GetByMemberAsync(memberId);
        return contributions.Select(c => (object)new
        {
            c.ContributionId, c.Period, c.EmployeeAmount,
            c.EmployerAmount, c.TotalAmount, c.PostedDate,
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
                e.EntryId, e.AccountId, EntryType = e.EntryType.ToString(),
                e.Amount, e.BalanceAfter, e.EntryDate, e.ReferenceId,
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
                c.ClaimId, ClaimType = c.ClaimType.ToString(),
                c.ClaimDate, c.EligibleAmount, c.VestedAmount,
                c.TaxDeductible, Status = c.Status.ToString()
            });
    }

    private static MemberResponse ToResponse(Member m) => new(
        m.MemberId, m.MembershipNumber, m.Name, m.DateOfBirth,
        m.Gender, m.NationalIdRef, m.EmployerId,
        m.Employer?.CompanyName ?? "", m.JoiningDate,
        m.DateOfRetirement, m.NomineeDetails, m.Status, m.User?.ProfileImageUrl);
}
