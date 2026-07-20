using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Contributions.Services.DTOs;
using Contributions.Domain.Entities;
using Contributions.Domain.Repositories;
using Contributions.Services;
using Contributions.Services.HttpClients;
using PensionVault.Shared.Contracts;

namespace Contributions.API.Controllers;

[ApiController]
[Route("api/ledger")]
[Authorize]
[Produces("application/json")]
public class LedgerController : ControllerBase
{
    private readonly ILedgerService _ledgerService;
    private readonly ILedgerRepository _ledgerRepo;
    private readonly IFundAccountRepository _accountRepo;
    private readonly IUnitOfWork _unitOfWork;

    public LedgerController(
        ILedgerService ledgerService,
        ILedgerRepository ledgerRepo,
        IFundAccountRepository accountRepo,
        IUnitOfWork unitOfWork)
    {
        _ledgerService = ledgerService;
        _ledgerRepo = ledgerRepo;
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [Authorize(Roles = "FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetAll()
        => Ok(await _ledgerService.GetAllLedgerEntriesAsync());

    [HttpGet("account/{accountId:guid}")]
    [Authorize(Roles = "Member,FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetLedger(Guid accountId, [FromServices] MemberServiceClient memberClient)
    {
        if (User.IsInRole("Member") && !await OwnsAccountAsync(accountId, memberClient))
            return Forbid();
        return Ok(await _ledgerService.GetAccountLedgerAsync(accountId));
    }

    [HttpPost("interest-credit")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> CreditInterest([FromBody] CreditInterestRequest request)
        => Ok(await _ledgerService.CreditInterestAsync(request));

    [HttpGet("interest-records/{accountId:guid}")]
    [Authorize(Roles = "Member,FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetInterestRecords(Guid accountId, [FromServices] MemberServiceClient memberClient)
    {
        if (User.IsInRole("Member") && !await OwnsAccountAsync(accountId, memberClient))
            return Forbid();
        return Ok(await _ledgerService.GetInterestRecordsAsync(accountId));
    }

    private async Task<bool> OwnsAccountAsync(Guid accountId, MemberServiceClient memberClient)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return false;
        var member = await memberClient.GetMemberByUserIdAsync(userId);
        if (member == null) return false;
        var account = await _accountRepo.FindByIdAsync(accountId);
        return account != null && account.MemberId == member.MemberId;
    }

    [HttpPost]
    public async Task<IActionResult> AddEntry([FromBody] CreateLedgerEntryRequest request)
    {
        var account = await _accountRepo.FindByIdAsync(request.AccountId);
        if (account == null) return NotFound("Account not found.");

        var entryType = Enum.Parse<EntryType>(request.EntryType, true);
        var entryStatus = string.IsNullOrEmpty(request.Status) 
            ? LedgerEntryStatus.Posted 
            : Enum.Parse<LedgerEntryStatus>(request.Status, true);

        var entry = new LedgerEntry
        {
            EntryId = Guid.NewGuid(),
            AccountId = request.AccountId,
            EntryType = entryType,
            Amount = request.Amount,
            ReferenceId = request.ReferenceId,
            Status = entryStatus,
            EntryDate = DateTime.UtcNow
        };

        if (entryType == EntryType.ClaimDebit)
        {
            account.TotalBalance -= request.Amount;
        }
        else if (entryType == EntryType.AnnuityDebit)
        {
            account.PensionBalance -= request.Amount;
        }

        entry.BalanceAfter = (entryType == EntryType.AnnuityDebit)
            ? account.PensionBalance
            : account.TotalBalance;

        await _ledgerRepo.AddEntryAsync(entry);
        await _unitOfWork.SaveChangesAsync();

        return Ok(entry);
    }
}



