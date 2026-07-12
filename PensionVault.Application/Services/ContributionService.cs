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
        if (request.TotalEmployeeShare <= 0)
            throw new ArgumentException("Total employee share must be greater than zero.");
        if (request.TotalEmployerShare <= 0)
            throw new ArgumentException("Total employer share must be greater than zero.");

        if (request.MemberContributions != null)
        {
            foreach (var item in request.MemberContributions)
            {
                if (item.EmployeeAmount <= 0)
                    throw new ArgumentException("Member employee contribution amount must be greater than zero.");
                if (item.EmployerAmount <= 0)
                    throw new ArgumentException("Member employer contribution amount must be greater than zero.");
            }
        }

        var employer = await _employerRepo.FindByIdAsync(request.EmployerId);
        // NOTE: In microservices architecture, employer lives in Members DB.
        // If the proxy returns null (cross-service), we continue without throwing.


        var total = request.TotalEmployeeShare + request.TotalEmployerShare;
        var remittance = new ContributionRemittance
        {
            EmployerId = request.EmployerId,
            RemittancePeriod = request.RemittancePeriod,
            TotalEmployeeShare = request.TotalEmployeeShare,
            TotalEmployerShare = request.TotalEmployerShare,
            TotalPensionAmount = request.TotalPensionAmount,
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
                PensionAmount = item.PensionAmount,
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

                // EPF contribution credit ledger entry
                await _ledgerRepo.AddEntryAsync(new LedgerEntry
                {
                    AccountId = account.AccountId,
                    EntryType = EntryType.ContributionCredit,
                    Amount = contribution.TotalAmount,
                    BalanceAfter = account.TotalBalance,
                    ReferenceId = remittance.RemittanceId.ToString(),
                    Status = LedgerEntryStatus.Posted
                });

                // EPS pension credit — tracked separately in PensionBalance
                if (item.PensionAmount > 0)
                {
                    account.PensionBalance += item.PensionAmount;
                    await _ledgerRepo.AddEntryAsync(new LedgerEntry
                    {
                        AccountId = account.AccountId,
                        EntryType = EntryType.PensionCredit,
                        Amount = item.PensionAmount,
                        BalanceAfter = account.PensionBalance,
                        ReferenceId = remittance.RemittanceId.ToString(),
                        Status = LedgerEntryStatus.Posted
                    });
                }
            }

            var member = await _memberRepo.FindByIdAsync(item.MemberId);
            if (member != null)
            {
                await _notificationRepo.AddAsync(new Notification
                {
                    UserId = member.UserId,
                    Message = $"A contribution of ₹{item.EmployeeAmount + item.EmployerAmount:N2} (Pension: ₹{item.PensionAmount:N2}) has been posted to your account for period {request.RemittancePeriod}.",
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

        var admins = await GetAdminUsersAsync();
        var adminNotifications = admins.Select(adminUser => new Notification
        {
            UserId = adminUser.UserId,
            Message = $"New remittance of ₹{total:N2} submitted for period {request.RemittancePeriod}. Awaiting reconciliation.",
            Category = NotificationCategory.Compliance,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });
        await _notificationRepo.AddRangeAsync(adminNotifications);

        await _unitOfWork.SaveChangesAsync();
        return await GetRemittanceAsync(remittance.RemittanceId);
    }

    public async Task<RemittanceResponse> GetRemittanceAsync(Guid remittanceId)
    {
        var r = await _contributionRepo.FindRemittanceByIdAsync(remittanceId)
            ?? throw new KeyNotFoundException("Remittance not found.");
        var employer = await _employerRepo.FindByIdAsync(r.EmployerId);
        return new RemittanceResponse(
            r.RemittanceId, r.EmployerId, employer?.CompanyName ?? "",
            r.RemittancePeriod, r.TotalEmployeeShare, r.TotalEmployerShare,
            r.TotalPensionAmount, r.TotalAmount, r.RemittanceDate, r.CoverageCount, r.Status);
    }

    public async Task<IEnumerable<RemittanceResponse>> GetEmployerRemittancesAsync(Guid employerId)
    {
        var remittances = await _contributionRepo.GetByEmployerAsync(employerId);
        var employer = await _employerRepo.FindByIdAsync(employerId);
        return remittances.Select(r => new RemittanceResponse(
            r.RemittanceId, r.EmployerId, employer?.CompanyName ?? "",
            r.RemittancePeriod, r.TotalEmployeeShare, r.TotalEmployerShare,
            r.TotalPensionAmount, r.TotalAmount, r.RemittanceDate, r.CoverageCount, r.Status));
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

        var admins = await GetAdminUsersAsync();
        var adminNotifications = admins.Select(adminUser => new Notification
        {
            UserId = adminUser.UserId,
            Message = $"Remittance for period {remittance.RemittancePeriod} has been reconciled. Status: {remittance.Status}.",
            Category = NotificationCategory.Compliance,
            Status = NotificationStatus.Unread,
            CreatedDate = DateTime.UtcNow
        });
        await _notificationRepo.AddRangeAsync(adminNotifications);

        await _unitOfWork.SaveChangesAsync();
        return await GetRemittanceAsync(remittanceId);
    }

    public async Task<IEnumerable<RemittanceResponse>> GetAllRemittancesAsync()
    {
        var remittances = await _contributionRepo.GetAllRemittancesAsync();
        var employers = await _employerRepo.GetAllAsync();
        var employerDict = employers.ToDictionary(e => e.EmployerId, e => e.CompanyName);
        return remittances.Select(r => new RemittanceResponse(
            r.RemittanceId, r.EmployerId, employerDict.TryGetValue(r.EmployerId, out var name) ? name : "",
            r.RemittancePeriod, r.TotalEmployeeShare, r.TotalEmployerShare,
            r.TotalPensionAmount, r.TotalAmount, r.RemittanceDate, r.CoverageCount, r.Status));
    }

    public async Task<IEnumerable<MemberContributionResponse>> GetMemberContributionsAsync(Guid memberId)
    {
        var member = await _memberRepo.FindByIdAsync(memberId);
        var contributions = await _contributionRepo.GetByMemberAsync(memberId);
        return contributions.Select(c => new MemberContributionResponse(
            c.ContributionId, c.MemberId, member?.Name ?? "",
            c.Period, c.EmployeeAmount, c.EmployerAmount, c.PensionAmount,
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
        var employers = await _employerRepo.GetAllAsync();
        var employerDict = employers.ToDictionary(e => e.EmployerId, e => e.CompanyName);
        return defaults.Select(r => new RemittanceResponse(
            r.RemittanceId, r.EmployerId, employerDict.TryGetValue(r.EmployerId, out var name) ? name : "",
            r.RemittancePeriod, r.TotalEmployeeShare, r.TotalEmployerShare,
            r.TotalPensionAmount, r.TotalAmount, r.RemittanceDate, r.CoverageCount, r.Status));
    }

    public async Task<IEnumerable<RemittanceResponse>> GetOverdueRemittancesAsync()
    {
        var overdue = await _contributionRepo.GetByStatusesAsync(
            RemittanceStatus.Received, RemittanceStatus.Shortfall, RemittanceStatus.Default);
        var employers = await _employerRepo.GetAllAsync();
        var employerDict = employers.ToDictionary(e => e.EmployerId, e => e.CompanyName);
        return overdue.Select(r => new RemittanceResponse(
            r.RemittanceId, r.EmployerId, employerDict.TryGetValue(r.EmployerId, out var name) ? name : "",
            r.RemittancePeriod, r.TotalEmployeeShare, r.TotalEmployerShare,
            r.TotalPensionAmount, r.TotalAmount, r.RemittanceDate, r.CoverageCount, r.Status));
    }

    public async Task<DefaulterSummaryResponse> GetDefaulterSummaryAsync(Guid employerId)
    {
        var employer = await _employerRepo.FindByIdAsync(employerId);

        var missingOrShortfall = await _contributionRepo.GetByStatusesAsync(RemittanceStatus.Default, RemittanceStatus.Shortfall);
        var employerDefaults = missingOrShortfall.Where(r => r.EmployerId == employerId).ToList();

        var allRemittances = await _contributionRepo.GetByEmployerAsync(employerId);
        var lastRemittance = allRemittances.FirstOrDefault();

        return new DefaulterSummaryResponse(
            employerId, employer?.CompanyName ?? "",
            employerDefaults.Count,
            employerDefaults.Sum(r => r.TotalAmount),
            lastRemittance?.RemittancePeriod ?? "None");
    }

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
