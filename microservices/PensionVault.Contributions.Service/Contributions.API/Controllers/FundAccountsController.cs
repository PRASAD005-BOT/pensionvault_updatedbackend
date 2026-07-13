using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contributions.Domain.Entities;
using Contributions.Domain.Repositories;
using Contributions.Data;
using Contributions.Services.HttpClients;
using PensionVault.Shared.Contracts;

namespace Contributions.API.Controllers;

[ApiController]
[Route("api/fundaccounts")]
[Authorize]
public class FundAccountsController : ControllerBase
{
    private readonly IFundAccountRepository _accountRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ContributionsDbContext _dbContext;
    private readonly MemberServiceClient _memberClient;

    public FundAccountsController(
        IFundAccountRepository accountRepo, 
        IUnitOfWork unitOfWork,
        ContributionsDbContext dbContext,
        MemberServiceClient memberClient)
    {
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _memberClient = memberClient;
    }

    [HttpGet("{accountId:guid}")]
    public async Task<IActionResult> GetById(Guid accountId)
    {
        var account = await _accountRepo.FindByIdAsync(accountId);
        if (account == null) return NotFound();
        return Ok(MapToResponse(account));
    }

    [HttpGet("active/member/{memberId:guid}")]
    public async Task<IActionResult> GetActiveByMember(Guid memberId)
    {
        var account = await _accountRepo.FindActiveByMemberAsync(memberId);
        if (account == null) return NotFound();
        return Ok(MapToResponse(account));
    }

    [HttpGet("member/{memberId:guid}")]
    public async Task<IActionResult> GetByMember(Guid memberId)
    {
        var accounts = await _accountRepo.GetByMemberAsync(memberId);
        return Ok(accounts.Select(MapToResponse));
    }

    [HttpGet("exists/member/{memberId:guid}")]
    public async Task<IActionResult> ExistsByMember(Guid memberId)
        => Ok(await _accountRepo.ExistsByMemberAsync(memberId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFundAccountRequest request)
    {
        // Ensure the scheme exists locally
        var schemeExists = await _dbContext.FundSchemes.AnyAsync(s => s.SchemeId == request.SchemeId);
        if (!schemeExists)
        {
            var schemeDetails = await _memberClient.GetSchemeByIdAsync(request.SchemeId);
            if (schemeDetails != null)
            {
                var localScheme = new FundScheme
                {
                    SchemeId = schemeDetails.SchemeId,
                    SchemeName = schemeDetails.SchemeName,
                    SchemeType = Enum.TryParse<SchemeType>(schemeDetails.SchemeType, true, out var st) ? st : SchemeType.EPF,
                    EmployeeContributionRate = schemeDetails.EmployeeContributionRate,
                    EmployerContributionRate = schemeDetails.EmployerContributionRate,
                    InterestRatePA = schemeDetails.InterestRatePA,
                    VestingSchedule = schemeDetails.VestingSchedule,
                    Status = Enum.TryParse<SchemeStatus>(schemeDetails.Status, true, out var ss) ? ss : SchemeStatus.Active
                };
                await _dbContext.FundSchemes.AddAsync(localScheme);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                return BadRequest($"Scheme {request.SchemeId} could not be resolved from Members Service.");
            }
        }

        var status = Enum.TryParse<FundAccountStatus>(request.Status, true, out var parsedStatus) ? parsedStatus : FundAccountStatus.Active;
        var account = new FundAccount
        {
            AccountId = Guid.NewGuid(),
            MemberId = request.MemberId,
            SchemeId = request.SchemeId,
            AccountOpenDate = DateTime.UtcNow,
            EmployeeContributionBalance = 0,
            EmployerContributionBalance = 0,
            PensionBalance = 0,
            InterestAccrued = 0,
            TotalBalance = 0,
            VestingPercent = request.VestingPercent,
            Status = status
        };

        await _accountRepo.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { accountId = account.AccountId }, new { account.AccountId });
    }

    private static FundAccountResponse MapToResponse(FundAccount a) => new(
        a.AccountId, a.MemberId, a.SchemeId,
        a.AccountOpenDate, a.EmployeeContributionBalance, a.EmployerContributionBalance,
        a.PensionBalance, a.InterestAccrued, a.TotalBalance, a.VestingPercent, a.Status.ToString()
    );
}



