using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PensionVault.Application.DTOs.Claims;
using PensionVault.Application.Interfaces;

namespace PensionVault.Claims.Service.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize]
[Produces("application/json")]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly IMemberService _memberService;
    public ClaimsController(IClaimService claimService, IMemberService memberService) 
    {
        _claimService = claimService;
        _memberService = memberService;
    }

    [HttpPost]
    [Authorize(Roles = "Member,FundAdmin")]
    public async Task<IActionResult> Submit([FromBody] CreateClaimRequest request)
    {
        if (User.IsInRole("Member"))
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out var userId))
            {
                var member = await _memberService.GetByUserIdAsync(userId);
                request = request with { MemberId = member.MemberId };
            }
        }
        var result = await _claimService.SubmitClaimAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.ClaimId }, result);
    }

    [HttpGet]
    [Authorize(Roles = "Member,FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetAll()
    {
        var allClaims = await _claimService.GetAllClaimsAsync();
        if (User.IsInRole("Member"))
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out var userId))
            {
                try {
                    var member = await _memberService.GetByUserIdAsync(userId);
                    return Ok(allClaims.Where(c => c.MemberId == member.MemberId));
                } catch { return Ok(new List<ClaimResponse>()); }
            }
        }
        return Ok(allClaims);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _claimService.GetClaimAsync(id));

    [HttpPut("{id:guid}/review")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Review(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _claimService.ReviewClaimAsync(id, userId));
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _claimService.ApproveClaimAsync(id, userId));
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _claimService.RejectClaimAsync(id, userId));
    }

    [HttpPost("{id:guid}/disburse")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Disburse(Guid id, [FromBody] DisburseClaimRequest request)
        => Ok(await _claimService.DisburseClaimAsync(id, request));

    [HttpPost("partial-withdrawal")]
    [Authorize(Roles = "Member,FundAdmin")]
    public async Task<IActionResult> SubmitPartialWithdrawal([FromBody] CreatePartialWithdrawalRequest request)
    {
        if (User.IsInRole("Member"))
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out var userId))
            {
                var member = await _memberService.GetByUserIdAsync(userId);
                request = request with { MemberId = member.MemberId };
            }
        }
        var result = await _claimService.SubmitPartialWithdrawalAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.ClaimId }, result);
    }

    [HttpPost("{id:guid}/disburse-partial-withdrawal")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> DisbursePartialWithdrawal(Guid id, [FromBody] DisbursePartialWithdrawalRequest request)
        => Ok(await _claimService.DisbursePartialWithdrawalAsync(id, request));
}

