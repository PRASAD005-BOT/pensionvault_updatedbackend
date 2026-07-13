using Contributions.Services.DTOs;
using Contributions.Domain.Entities;
using Contributions.Domain.Repositories;
using Contributions.Services.HttpClients;
using PensionVault.Shared.Contracts;

namespace Contributions.Services;

public class ContributionService : IContributionService
{
    private readonly IContributionRepository _contributionRepo;
    private readonly IFundAccountRepository _accountRepo;
    private readonly ILedgerRepository _ledgerRepo;
    private readonly MemberServiceClient _memberClient;
    private readonly NotificationServiceClient _notificationClient;
    private readonly IUnitOfWork _unitOfWork;

    public ContributionService(
        IContributionRepository contributionRepo,
        IFundAccountRepository accountRepo,
        ILedgerRepository ledgerRepo,
        MemberServiceClient memberClient,
        NotificationServiceClient notificationClient,
        IUnitOfWork unitOfWork)
    {
        _contributionRepo = contributionRepo;
        _accountRepo = accountRepo;
        _ledgerRepo = ledgerRepo;
        _memberClient = memberClient;
        _notificationClient = notificationClient;
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

        var notifications = new List<CreateNotificationRequest>();

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

                // EPS pension credit
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

            var member = await _memberClient.GetMemberByIdAsync(item.MemberId);
            if (member != null)
            {
                notifications.Add(new CreateNotificationRequest(
                    member.UserId,
                    $"A contribution of ₹{item.EmployeeAmount + item.EmployerAmount:N2} (Pension: ₹{item.PensionAmount:N2}) has been posted to your account for period {request.RemittancePeriod}.",
                    "Contribution"));
            }
        }

        // We can look up representatives or defaults. Let's just lookup representatives if we can or proceed
        // If employer lookup is unsuccessful, it will proceed silently or log.
        // Let's send the notifications to Member and admins
        var admins = await GetAdminUsersAsync();
        notifications.AddRange(admins.Select(adminUser => new CreateNotificationRequest(
            adminUser.UserId,
            $"New remittance of ₹{total:N2} submitted for period {request.RemittancePeriod}. Awaiting reconciliation.",
            "Compliance")));

        await _notificationClient.SendBulkNotificationsAsync(notifications);
        await _unitOfWork.SaveChangesAsync();

        return await GetRemittanceAsync(remittance.RemittanceId);
    }

    public async Task<RemittanceResponse> GetRemittanceAsync(Guid remittanceId)
    {
        var r = await _contributionRepo.FindRemittanceByIdAsync(remittanceId)
            ?? throw new KeyNotFoundException("Remittance not found.");
        var employer = await _memberClient.GetEmployerByIdAsync(r.EmployerId);
        return new RemittanceResponse(
            r.RemittanceId, r.EmployerId, employer?.CompanyName ?? "",
            r.RemittancePeriod, r.TotalEmployeeShare, r.TotalEmployerShare,
            r.TotalPensionAmount, r.TotalAmount, r.RemittanceDate, r.CoverageCount, r.Status);
    }

    public async Task<IEnumerable<RemittanceResponse>> GetEmployerRemittancesAsync(Guid employerId)
    {
        var remittances = await _contributionRepo.GetByEmployerAsync(employerId);
        var employer = await _memberClient.GetEmployerByIdAsync(employerId);
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

        var notifications = new List<CreateNotificationRequest>();

        var admins = await GetAdminUsersAsync();
        notifications.AddRange(admins.Select(adminUser => new CreateNotificationRequest(
            adminUser.UserId,
            $"Remittance for period {remittance.RemittancePeriod} has been reconciled. Status: {remittance.Status}.",
            "Compliance")));

        await _notificationClient.SendBulkNotificationsAsync(notifications);
        await _unitOfWork.SaveChangesAsync();

        return await GetRemittanceAsync(remittanceId);
    }

    public async Task<IEnumerable<RemittanceResponse>> GetAllRemittancesAsync()
    {
        var remittances = await _contributionRepo.GetAllRemittancesAsync();
        var employers = await _memberClient.GetAllEmployersAsync();
        var employerDict = employers.ToDictionary(e => e.EmployerId, e => e.CompanyName);
        return remittances.Select(r => new RemittanceResponse(
            r.RemittanceId, r.EmployerId, employerDict.TryGetValue(r.EmployerId, out var name) ? name : "",
            r.RemittancePeriod, r.TotalEmployeeShare, r.TotalEmployerShare,
            r.TotalPensionAmount, r.TotalAmount, r.RemittanceDate, r.CoverageCount, r.Status));
    }

    public async Task<IEnumerable<MemberContributionResponse>> GetMemberContributionsAsync(Guid memberId)
    {
        var member = await _memberClient.GetMemberByIdAsync(memberId);
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
        var employers = await _memberClient.GetAllEmployersAsync();
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
        var employers = await _memberClient.GetAllEmployersAsync();
        var employerDict = employers.ToDictionary(e => e.EmployerId, e => e.CompanyName);
        return overdue.Select(r => new RemittanceResponse(
            r.RemittanceId, r.EmployerId, employerDict.TryGetValue(r.EmployerId, out var name) ? name : "",
            r.RemittancePeriod, r.TotalEmployeeShare, r.TotalEmployerShare,
            r.TotalPensionAmount, r.TotalAmount, r.RemittanceDate, r.CoverageCount, r.Status));
    }

    public async Task<DefaulterSummaryResponse> GetDefaerSummaryAsync(Guid employerId)
    {
        var employer = await _memberClient.GetEmployerByIdAsync(employerId);

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

    public Task<DefaulterSummaryResponse> GetDefaulterSummaryAsync(Guid employerId)
        => GetDefaerSummaryAsync(employerId);

    private async Task<List<UserSummaryResponse>> GetAdminUsersAsync()
    {
        // Simple fallback if call fails - fetch via HTTP client to Members service
        try
        {
            var admins = await _memberClient.GetUsersByRoleAsync("Admin");
            var fundAdmins = await _memberClient.GetUsersByRoleAsync("FundAdmin");
            var compliance = await _memberClient.GetUsersByRoleAsync("Compliance");
            
            var all = new List<UserSummaryResponse>();
            all.AddRange(admins);
            all.AddRange(fundAdmins);
            all.AddRange(compliance);
            return all.GroupBy(u => u.UserId).Select(g => g.First()).ToList();
        }
        catch
        {
            return new List<UserSummaryResponse>();
        }
    }
}





