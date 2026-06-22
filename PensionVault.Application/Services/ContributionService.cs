using PensionVault.Application.DTOs.Contributions;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Application.Services;

public class ContributionService : IContributionService
{
    private readonly IContributionRepository _contributionRepo;
    private readonly IEmployerRepository _employerRepo;
    private readonly IMemberRepository _memberRepo;
    private readonly IFundAccountRepository _accountRepo;
    private readonly ILedgerRepository _ledgerRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ContributionService(
        IContributionRepository contributionRepo,
        IEmployerRepository employerRepo,
        IMemberRepository memberRepo,
        IFundAccountRepository accountRepo,
        ILedgerRepository ledgerRepo,
        INotificationRepository notificationRepo,
        IUserRepository userRepo,
        IUnitOfWork unitOfWork)
    {
        _contributionRepo = contributionRepo;
        _employerRepo = employerRepo;
        _memberRepo = memberRepo;
        _accountRepo = accountRepo;
        _ledgerRepo = ledgerRepo;
        _notificationRepo = notificationRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<RemittanceResponse> CreateRemittanceAsync(CreateRemittanceRequest request)
    {
        var employer = await _employerRepo.FindByIdAsync(request.EmployerId)
            ?? throw new KeyNotFoundException("Employer not found.");

        var total = request.TotalEmployeeShare + request.TotalEmployerShare;
        var remittance = new ContributionRemittance
        {
            EmployerId = request.EmployerId,
            RemittancePeriod = request.RemittancePeriod,
            TotalEmployeeShare = request.TotalEmployeeShare,
            TotalEmployerShare = request.TotalEmployerShare,
            TotalAmount = total,
            RemittanceDate = DateTime.UtcNow,
            CoverageCount = request.CoverageCount,
            Status = RemittanceStatus.Received
        };
        await _contributionRepo.AddRemittanceAsync(remittance);

        foreach (var item in request.MemberContributions)
        {
            var contribution = new MemberContribution
            {
                RemittanceId = remittance.RemittanceId,
                MemberId = item.MemberId,
                Period = request.RemittancePeriod,
                EmployeeAmount = item.EmployeeAmount,
                EmployerAmount = item.EmployerAmount,
                TotalAmount = item.EmployeeAmount + item.EmployerAmount,
                PostedDate = DateTime.UtcNow,
                Status = ContributionStatus.Posted
            };
            await _contributionRepo.AddContributionAsync(contribution);

            var account = await _accountRepo.FindActiveByMemberAsync(item.MemberId);
            if (account != null)
            {
                account.EmployeeContributionBalance += item.EmployeeAmount;
                account.EmployerContributionBalance += item.EmployerAmount;
                account.TotalBalance += contribution.TotalAmount;
                await _ledgerRepo.AddEntryAsync(new LedgerEntry
                {
                    AccountId = account.AccountId,
                    EntryType = EntryType.ContributionCredit,
                    Amount = contribution.TotalAmount,
                    BalanceAfter = account.TotalBalance,
                    ReferenceId = remittance.RemittanceId.ToString(),
                    Status = LedgerEntryStatus.Posted
                });
            }

            var member = await _memberRepo.FindByIdAsync(item.MemberId);
            if (member != null)
            {
                await _notificationRepo.AddAsync(new Notification
                {
                    UserId = member.UserId,
                    Message = $"A contribution of ₹{item.EmployeeAmount + item.EmployerAmount:N2} has been posted to your account for period {request.RemittancePeriod}.",
                    Category = NotificationCategory.Contribution,
                    Status = NotificationStatus.Unread,
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        var employerUsers = await _userRepo.GetByOrgAndRoleAsync(request.EmployerId, UserRole.Employer);
        var empNotifications = employerUsers.Select(u => new Notification
        {
            UserId = u.UserId,
            Message = $"Remittance of ₹{total:N2} for period {request.RemittancePeriod} has been submitted successfully.",
            Category = NotificationCategory.Contribution,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });
        await _notificationRepo.AddRangeAsync(empNotifications);

        await _unitOfWork.SaveChangesAsync();
        return await GetRemittanceAsync(remittance.RemittanceId);
    }

    public async Task<RemittanceResponse> GetRemittanceAsync(Guid remittanceId)
    {
        var r = await _contributionRepo.FindRemittanceByIdAsync(remittanceId)
            ?? throw new KeyNotFoundException("Remittance not found.");
        return ToResponse(r);
    }

    public async Task<IEnumerable<RemittanceResponse>> GetEmployerRemittancesAsync(Guid employerId)
    {
        var remittances = await _contributionRepo.GetByEmployerAsync(employerId);
        return remittances.Select(ToResponse);
    }

    public async Task<RemittanceResponse> ReconcileAsync(Guid remittanceId)
    {
        var remittance = await _contributionRepo.FindRemittanceByIdAsync(remittanceId)
            ?? throw new KeyNotFoundException("Remittance not found.");

        var postedCount = await _contributionRepo.CountPostedContributionsAsync(remittanceId);
        remittance.Status = postedCount == remittance.CoverageCount
            ? RemittanceStatus.Reconciled
            : RemittanceStatus.Shortfall;

        var employerUsers = await _userRepo.GetByOrgAndRoleAsync(remittance.EmployerId, UserRole.Employer);
        var notifications = employerUsers.Select(u => new Notification
        {
            UserId = u.UserId,
            Message = $"Your remittance for period {remittance.RemittancePeriod} has been reconciled. Status: {remittance.Status}.",
            Category = NotificationCategory.Contribution,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });
        await _notificationRepo.AddRangeAsync(notifications);

        await _unitOfWork.SaveChangesAsync();
        return await GetRemittanceAsync(remittanceId);
    }

    public async Task<IEnumerable<RemittanceResponse>> GetAllRemittancesAsync()
    {
        var remittances = await _contributionRepo.GetAllRemittancesAsync();
        return remittances.Select(ToResponse);
    }

    public async Task<IEnumerable<MemberContributionResponse>> GetMemberContributionsAsync(Guid memberId)
    {
        var contributions = await _contributionRepo.GetByMemberAsync(memberId);
        return contributions.Select(c => new MemberContributionResponse(
            c.ContributionId, c.MemberId, c.Member.Name,
            c.Period, c.EmployeeAmount, c.EmployerAmount,
            c.TotalAmount, c.PostedDate, c.Status));
    }

    public async Task<ReconciliationReportResponse> GetReconciliationReportAsync(Guid remittanceId)
    {
        var remittance = await _contributionRepo.FindRemittanceByIdAsync(remittanceId)
            ?? throw new KeyNotFoundException("Remittance not found.");

        var reconciledCount = await _contributionRepo.CountPostedContributionsAsync(remittanceId);
        var reconciledAmount = await _contributionRepo.SumReconciledAmountAsync(remittanceId);

        return new ReconciliationReportResponse(
            remittance.RemittanceId, remittance.RemittancePeriod,
            remittance.CoverageCount, reconciledCount,
            remittance.TotalAmount, reconciledAmount,
            remittance.Status.ToString());
    }

    public async Task<IEnumerable<RemittanceResponse>> GetDefaultersAsync()
    {
        var defaults = await _contributionRepo.GetByStatusesAsync(RemittanceStatus.Default, RemittanceStatus.Shortfall);
        return defaults.Select(ToResponse);
    }

    public async Task<IEnumerable<RemittanceResponse>> GetOverdueRemittancesAsync()
    {
        var overdue = await _contributionRepo.GetByStatusesAsync(
            RemittanceStatus.Received, RemittanceStatus.Shortfall, RemittanceStatus.Default);
        return overdue.Select(ToResponse);
    }

    public async Task<DefaulterSummaryResponse> GetDefaulterSummaryAsync(Guid employerId)
    {
        var employer = await _employerRepo.FindByIdAsync(employerId)
            ?? throw new KeyNotFoundException("Employer not found.");

        var missingOrShortfall = await _contributionRepo.GetByStatusesAsync(RemittanceStatus.Default, RemittanceStatus.Shortfall);
        var employerDefaults = missingOrShortfall.Where(r => r.EmployerId == employerId).ToList();

        var allRemittances = await _contributionRepo.GetByEmployerAsync(employerId);
        var lastRemittance = allRemittances.FirstOrDefault();

        return new DefaulterSummaryResponse(
            employerId, employer.CompanyName,
            employerDefaults.Count,
            employerDefaults.Sum(r => r.TotalAmount),
            lastRemittance?.RemittancePeriod ?? "None");
    }

    private static RemittanceResponse ToResponse(ContributionRemittance r) => new(
        r.RemittanceId, r.EmployerId, r.Employer?.CompanyName ?? "",
        r.RemittancePeriod, r.TotalEmployeeShare, r.TotalEmployerShare,
        r.TotalAmount, r.RemittanceDate, r.CoverageCount, r.Status);
}
