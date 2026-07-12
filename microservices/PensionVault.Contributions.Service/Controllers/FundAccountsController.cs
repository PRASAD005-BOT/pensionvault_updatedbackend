using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionVault.Domain.Entities;
using PensionVault.Domain.Interfaces;

namespace PensionVault.Contributions.Service.Controllers;

[ApiController]
[Route("api/fundaccounts")]
[Authorize]
public class FundAccountsController : ControllerBase
{
    private readonly IFundAccountRepository _accountRepo;
    private readonly IUnitOfWork _unitOfWork;

    public FundAccountsController(IFundAccountRepository accountRepo, IUnitOfWork unitOfWork)
    {
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("{accountId:guid}")]
    public async Task<IActionResult> GetById(Guid accountId)
    {
        var account = await _accountRepo.FindByIdAsync(accountId);
        if (account == null) return NotFound();
        return Ok(account);
    }

    [HttpGet("active/member/{memberId:guid}")]
    public async Task<IActionResult> GetActiveByMember(Guid memberId)
    {
        var account = await _accountRepo.FindActiveByMemberAsync(memberId);
        if (account == null) return NotFound();
        return Ok(account);
    }

    [HttpGet("member/{memberId:guid}")]
    public async Task<IActionResult> GetByMember(Guid memberId)
        => Ok(await _accountRepo.GetByMemberAsync(memberId));

    [HttpGet("exists/member/{memberId:guid}")]
    public async Task<IActionResult> ExistsByMember(Guid memberId)
        => Ok(await _accountRepo.ExistsByMemberAsync(memberId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FundAccount account)
    {
        await _accountRepo.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { accountId = account.AccountId }, new { account.AccountId });
    }
}

