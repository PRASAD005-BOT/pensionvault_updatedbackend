using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionVault.Application.DTOs.Ledger;
using PensionVault.Application.Interfaces;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Enums;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Contributions.Service.Controllers;

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
    public async Task<IActionResult> GetLedger(Guid accountId)
        => Ok(await _ledgerService.GetAccountLedgerAsync(accountId));

    [HttpPost("interest-credit")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> CreditInterest([FromBody] CreditInterestRequest request)
        => Ok(await _ledgerService.CreditInterestAsync(request));

    [HttpGet("interest-records/{accountId:guid}")]
    public async Task<IActionResult> GetInterestRecords(Guid accountId)
        => Ok(await _ledgerService.GetInterestRecordsAsync(accountId));

    [HttpPost]
    public async Task<IActionResult> AddEntry([FromBody] LedgerEntry entry)
    {
        var account = await _accountRepo.FindByIdAsync(entry.AccountId);
        if (account != null)
        {
            if (entry.EntryType == EntryType.ClaimDebit)
            {
                account.TotalBalance -= entry.Amount;
            }
            else if (entry.EntryType == EntryType.AnnuityDebit)
            {
                account.PensionBalance -= entry.Amount;
            }

            entry.BalanceAfter = (entry.EntryType == EntryType.AnnuityDebit)
                ? account.PensionBalance
                : account.TotalBalance;

            await _ledgerRepo.AddEntryAsync(entry);
            await _unitOfWork.SaveChangesAsync();
        }
        return Ok(entry);
    }
}

