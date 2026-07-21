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
    private readonly IShortfallRequestRepository _shortfallRepo;
    private readonly MemberServiceClient _memberClient;
    private readonly NotificationServiceClient _notificationClient;
    private readonly IUnitOfWork _unitOfWork;

    public ContributionService(
        IContributionRepository contributionRepo,
        IFundAccountRepository accountRepo,
        ILedgerRepository ledgerRepo,
        IShortfallRequestRepository shortfallRepo,
        MemberServiceClient memberClient,
        NotificationServiceClient notificationClient,
        IUnitOfWork unitOfWork)
    {
        _contributionRepo = contributionRepo;
        _accountRepo = accountRepo;
        _ledgerRepo = ledgerRepo;
        _shortfallRepo = shortfallRepo;
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

        // Save member contributions as Pending — ledger posting happens on Reconciliation
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
                Status = ContributionStatus.Pending
            };
            await _contributionRepo.AddContributionAsync(contribution);
        }

        var notifications = new List<CreateNotificationRequest>();
        var admins = await GetAdminUsersAsync();
        notifications.AddRange(admins.Select(adminUser => new CreateNotificationRequest(
            adminUser.UserId,
            $"New remittance of ₹{total:N2} submitted for period {request.RemittancePeriod}. Awaiting reconciliation.",
            "Compliance")));

        await _unitOfWork.SaveChangesAsync();
        await _notificationClient.SendBulkNotificationsAsync(notifications);

        return await GetRemittanceAsync(remittance.RemittanceId);
    }

    public async Task<RemittanceResponse> ReconcileAsync(Guid remittanceId)
    {
        var remittance = await _contributionRepo.FindRemittanceByIdAsync(remittanceId)
            ?? throw new KeyNotFoundException("Remittance not found.");

        if (remittance.Status == RemittanceStatus.Reconciled)
            throw new InvalidOperationException("Remittance has already been reconciled.");

        var notifications = new List<CreateNotificationRequest>();

        // Post member contributions and update account balances upon Fund Admin reconciliation
        foreach (var contribution in remittance.MemberContributions)
        {
            contribution.Status = ContributionStatus.Posted;
            contribution.PostedDate = DateTime.UtcNow;

            var account = await _accountRepo.FindActiveByMemberAsync(contribution.MemberId);
            if (account != null)
            {
                account.EmployeeContributionBalance += contribution.EmployeeAmount;
                account.EmployerContributionBalance += contribution.EmployerAmount;
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

                // EPS pension credit ledger entry
                if (contribution.PensionAmount > 0)
                {
                    account.PensionBalance += contribution.PensionAmount;
                    await _ledgerRepo.AddEntryAsync(new LedgerEntry
                    {
                        AccountId = account.AccountId,
                        EntryType = EntryType.PensionCredit,
                        Amount = contribution.PensionAmount,
                        BalanceAfter = account.PensionBalance,
                        ReferenceId = remittance.RemittanceId.ToString(),
                        Status = LedgerEntryStatus.Posted
                    });
                }
            }

            var member = await _memberClient.GetMemberByIdAsync(contribution.MemberId);
            if (member != null)
            {
                notifications.Add(new CreateNotificationRequest(
                    member.UserId,
                    $"A contribution of ₹{contribution.TotalAmount:N2} (Pension: ₹{contribution.PensionAmount:N2}) has been posted to your account for period {remittance.RemittancePeriod}.",
                    "Contribution"));
            }
        }

        var postedCount = remittance.MemberContributions.Count(c => c.Status == ContributionStatus.Posted);
        remittance.Status = postedCount == remittance.CoverageCount
            ? RemittanceStatus.Reconciled
            : RemittanceStatus.Shortfall;

        var admins = await GetAdminUsersAsync();
        notifications.AddRange(admins.Select(adminUser => new CreateNotificationRequest(
            adminUser.UserId,
            $"Remittance for period {remittance.RemittancePeriod} has been reconciled. Status: {remittance.Status}.",
            "Compliance")));

        await _unitOfWork.SaveChangesAsync();
        await _notificationClient.SendBulkNotificationsAsync(notifications);

        return await GetRemittanceAsync(remittanceId);
    }

    public async Task<IEnumerable<MemberContributionResponse>> GetMemberContributionsAsync(Guid memberId)
    {
        var member = await _memberClient.GetMemberByIdAsync(memberId);
        var contributions = await _contributionRepo.GetByMemberAsync(memberId);

        // Only return contributions that have been reconciled and posted
        return contributions
            .Where(c => c.Status == ContributionStatus.Posted)
            .Select(c => new MemberContributionResponse(
                c.ContributionId, c.MemberId, member?.Name ?? "",
                c.Period, c.EmployeeAmount, c.EmployerAmount, c.PensionAmount,
                c.TotalAmount, c.PostedDate, c.Status));
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

    public async Task<IEnumerable<MemberShortfallResponse>> GetMemberShortfallsAsync(Guid memberId)
    {
        var member = await _memberClient.GetMemberByIdAsync(memberId);
        if (member == null) return Enumerable.Empty<MemberShortfallResponse>();

        var flagged = (await _contributionRepo.GetByStatusesAsync(RemittanceStatus.Shortfall, RemittanceStatus.Default))
            .Where(r => r.EmployerId == member.EmployerId)
            .ToList();
        if (flagged.Count == 0) return Enumerable.Empty<MemberShortfallResponse>();

        var myContributions = await _contributionRepo.GetByMemberAsync(memberId);
        var postedPeriods = myContributions.Where(c => c.Status == ContributionStatus.Posted).Select(c => c.Period).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return flagged
            .Where(r => !postedPeriods.Contains(r.RemittancePeriod))
            .OrderByDescending(r => r.RemittanceDate)
            .Select(r => new MemberShortfallResponse(
                r.RemittanceId,
                r.RemittancePeriod,
                r.Status.ToString(),
                $"No contribution was posted for you in {r.RemittancePeriod} — your employer's remittance for this period was flagged as {r.Status}. Contact your Fund Administrator if this looks wrong."));
    }

    private async Task<List<UserSummaryResponse>> GetAdminUsersAsync()
    {
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

    public async Task<ShortfallRequestResponse> RaiseShortfallAsync(Guid memberId, CreateShortfallRequest request)
    {
        var contribution = await _contributionRepo.FindContributionByIdAsync(request.ContributionId)
            ?? throw new KeyNotFoundException("Contribution not found.");
        if (contribution.MemberId != memberId)
            throw new UnauthorizedAccessException("This contribution does not belong to you.");
        if (contribution.Status != ContributionStatus.Posted)
            throw new InvalidOperationException("Shortfalls can only be raised on posted contribution transactions.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Please explain why you're raising this shortfall.");

        // Check existing shortfall requests for this contribution
        var existingRequests = await _shortfallRepo.GetByMemberAsync(memberId);
        var requestsForContribution = existingRequests.Where(s => s.ContributionId == request.ContributionId).ToList();

        if (requestsForContribution.Any(s => s.Status == ShortfallRequestStatus.Raised))
        {
            throw new InvalidOperationException("A shortfall request for this contribution is currently pending review.");
        }

        if (requestsForContribution.Any(s => s.Status == ShortfallRequestStatus.Rejected))
        {
            throw new InvalidOperationException("A shortfall request for this contribution was rejected by an authority and cannot be raised again.");
        }

        var member = await _memberClient.GetMemberByIdAsync(memberId);

        var shortfall = new ShortfallRequest
        {
            ContributionId = contribution.ContributionId,
            MemberId = memberId,
            EmployerId = member?.EmployerId ?? Guid.Empty,
            Reason = request.Reason,
            Status = ShortfallRequestStatus.Raised,
            RaisedDate = DateTime.UtcNow
        };
        await _shortfallRepo.AddAsync(shortfall);
        await _unitOfWork.SaveChangesAsync();

        var admins = await GetAdminUsersAsync();
        var notifications = admins.Select(a => new CreateNotificationRequest(
            a.UserId,
            $"{member?.Name ?? "A member"} raised a shortfall issue on their {contribution.Period} contribution: \"{request.Reason}\"",
            "Contribution")).ToList();
        if (notifications.Count > 0)
            await _notificationClient.SendBulkNotificationsAsync(notifications);

        return await ToShortfallResponseAsync(shortfall, contribution, member);
    }

    public async Task<ShortfallRequestResponse> ResolveShortfallAsync(Guid shortfallRequestId, ResolveShortfallRequest request)
    {
        var shortfall = await _shortfallRepo.FindByIdAsync(shortfallRequestId)
            ?? throw new KeyNotFoundException("Shortfall request not found.");
        if (shortfall.Status != ShortfallRequestStatus.Raised)
            throw new InvalidOperationException("Only a raised shortfall request can be resolved.");
        if (string.IsNullOrWhiteSpace(request.ResolutionNote))
            throw new ArgumentException("Please explain how this shortfall was resolved.");

        var contribution = await _contributionRepo.FindContributionByIdAsync(shortfall.ContributionId)
            ?? throw new KeyNotFoundException("Contribution not found.");

        var newEmployee = request.NewEmployeeAmount ?? contribution.EmployeeAmount;
        var newEmployer = request.NewEmployerAmount ?? contribution.EmployerAmount;
        var newPension = request.NewPensionAmount ?? contribution.PensionAmount;
        var deltaEmployee = newEmployee - contribution.EmployeeAmount;
        var deltaEmployer = newEmployer - contribution.EmployerAmount;
        var deltaPension = newPension - contribution.PensionAmount;
        var deltaTotal = deltaEmployee + deltaEmployer;

        if (deltaEmployee != 0 || deltaEmployer != 0 || deltaPension != 0)
        {
            contribution.EmployeeAmount = newEmployee;
            contribution.EmployerAmount = newEmployer;
            contribution.PensionAmount = newPension;
            contribution.TotalAmount = newEmployee + newEmployer;

            var account = await _accountRepo.FindActiveByMemberAsync(contribution.MemberId);
            if (account != null)
            {
                account.EmployeeContributionBalance += deltaEmployee;
                account.EmployerContributionBalance += deltaEmployer;
                account.TotalBalance += deltaTotal;

                if (deltaTotal != 0)
                {
                    await _ledgerRepo.AddEntryAsync(new LedgerEntry
                    {
                        AccountId = account.AccountId,
                        EntryType = EntryType.ContributionCredit,
                        Amount = deltaTotal,
                        BalanceAfter = account.TotalBalance,
                        ReferenceId = contribution.ContributionId.ToString(),
                        Status = LedgerEntryStatus.Posted
                    });
                }

                if (deltaPension != 0)
                {
                    account.PensionBalance += deltaPension;
                    await _ledgerRepo.AddEntryAsync(new LedgerEntry
                    {
                        AccountId = account.AccountId,
                        EntryType = EntryType.PensionCredit,
                        Amount = deltaPension,
                        BalanceAfter = account.PensionBalance,
                        ReferenceId = contribution.ContributionId.ToString(),
                        Status = LedgerEntryStatus.Posted
                    });
                }
            }
        }

        shortfall.Status = ShortfallRequestStatus.Resolved;
        shortfall.ResolutionNote = request.ResolutionNote;
        shortfall.ResolvedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        var member = await _memberClient.GetMemberByIdAsync(contribution.MemberId);
        if (member != null)
        {
            await _notificationClient.SendBulkNotificationsAsync(new List<CreateNotificationRequest>
            {
                new CreateNotificationRequest(
                    member.UserId,
                    $"Your shortfall issue for {contribution.Period} has been fixed: {request.ResolutionNote}",
                    "Contribution")
            });
        }

        return await ToShortfallResponseAsync(shortfall, contribution, member);
    }

    public async Task<ShortfallRequestResponse> RejectShortfallAsync(Guid shortfallRequestId, RejectShortfallRequest request)
    {
        var shortfall = await _shortfallRepo.FindByIdAsync(shortfallRequestId)
            ?? throw new KeyNotFoundException("Shortfall request not found.");
        if (shortfall.Status != ShortfallRequestStatus.Raised)
            throw new InvalidOperationException("Only a raised shortfall request can be rejected.");
        if (string.IsNullOrWhiteSpace(request.RejectionNote))
            throw new ArgumentException("Please explain why this shortfall request is being rejected.");

        var contribution = await _contributionRepo.FindContributionByIdAsync(shortfall.ContributionId)
            ?? throw new KeyNotFoundException("Contribution not found.");

        shortfall.Status = ShortfallRequestStatus.Rejected;
        shortfall.ResolutionNote = request.RejectionNote;
        shortfall.ResolvedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        var member = await _memberClient.GetMemberByIdAsync(contribution.MemberId);
        if (member != null)
        {
            await _notificationClient.SendBulkNotificationsAsync(new List<CreateNotificationRequest>
            {
                new CreateNotificationRequest(
                    member.UserId,
                    $"Your shortfall request for {contribution.Period} was rejected: {request.RejectionNote}",
                    "Contribution")
            });
        }

        return await ToShortfallResponseAsync(shortfall, contribution, member);
    }

    public async Task<IEnumerable<ShortfallRequestResponse>> GetAllShortfallRequestsAsync()
        => await BuildShortfallResponsesAsync(await _shortfallRepo.GetAllAsync());

    public async Task<IEnumerable<ShortfallRequestResponse>> GetShortfallRequestsByEmployerAsync(Guid employerId)
        => await BuildShortfallResponsesAsync(await _shortfallRepo.GetByEmployerAsync(employerId));

    public async Task<IEnumerable<ShortfallRequestResponse>> GetShortfallRequestsByMemberAsync(Guid memberId)
        => await BuildShortfallResponsesAsync(await _shortfallRepo.GetByMemberAsync(memberId));

    private async Task<IEnumerable<ShortfallRequestResponse>> BuildShortfallResponsesAsync(List<ShortfallRequest> requests)
    {
        var responses = new List<ShortfallRequestResponse>();
        foreach (var s in requests)
        {
            var contribution = await _contributionRepo.FindContributionByIdAsync(s.ContributionId);
            if (contribution == null) continue;
            var member = await _memberClient.GetMemberByIdAsync(s.MemberId);
            responses.Add(await ToShortfallResponseAsync(s, contribution, member));
        }
        return responses;
    }

    private async Task<ShortfallRequestResponse> ToShortfallResponseAsync(ShortfallRequest s, MemberContribution contribution, MemberResponse? member)
    {
        var employer = await _memberClient.GetEmployerByIdAsync(s.EmployerId);
        return new ShortfallRequestResponse(
            s.ShortfallRequestId, s.ContributionId, s.MemberId, member?.Name ?? "",
            contribution.Period, contribution.EmployeeAmount, contribution.EmployerAmount,
            contribution.PensionAmount, contribution.TotalAmount,
            s.EmployerId, employer?.CompanyName ?? "",
            s.Reason, s.Status.ToString(), s.RaisedDate, s.ResolutionNote, s.ResolvedDate);
    }
}