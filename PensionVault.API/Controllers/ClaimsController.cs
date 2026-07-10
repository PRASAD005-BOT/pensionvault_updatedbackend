using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PensionVault.Application.DTOs.Claims;
using PensionVault.Application.Interfaces;

namespace PensionVault.API.Controllers;

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
        try
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { status = 400, error = ex.Message, timestamp = DateTime.UtcNow });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { status = 400, error = ex.Message, timestamp = DateTime.UtcNow });
        }
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
                try
                {
                    var member = await _memberService.GetByUserIdAsync(userId);
                    return Ok(allClaims.Where(c => c.MemberId == member.MemberId));
                }
                catch { return Ok(new List<ClaimResponse>()); }
            }
        }
        return Ok(allClaims);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _claimService.GetClaimAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { status = 404, error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    [HttpPut("{id:guid}/review")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Review(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _claimService.ReviewClaimAsync(id, userId));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { status = 409, error = ex.Message, timestamp = DateTime.UtcNow });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { status = 404, error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _claimService.ApproveClaimAsync(id, userId));
        }
        catch (InvalidOperationException ex)
        {
            // Maps Bug #4 state conflicts cleanly to HTTP 409 payloads
            return Conflict(new { status = 409, error = ex.Message, timestamp = DateTime.UtcNow });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { status = 404, error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Reject(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _claimService.RejectClaimAsync(id, userId));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { status = 409, error = ex.Message, timestamp = DateTime.UtcNow });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { status = 404, error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    [HttpPost("{id:guid}/disburse")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Disburse(Guid id, [FromBody] DisburseClaimRequest request)
    {
        try
        {
            return Ok(await _claimService.DisburseClaimAsync(id, request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { status = 400, error = ex.Message, timestamp = DateTime.UtcNow });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { status = 400, error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    [HttpPost("partial-withdrawal")]
    [Authorize(Roles = "Member,FundAdmin")]
    public async Task<IActionResult> SubmitPartialWithdrawal([FromBody] CreatePartialWithdrawalRequest request)
    {
        try
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
        catch (ArgumentException ex)
        {
            // Catches Bug #1, Bug #2, Bug #5, Bug #7, Bug #8 instantly at the gate
            return BadRequest(new { status = 400, error = ex.Message, timestamp = DateTime.UtcNow });
        }
        catch (InvalidOperationException ex)
        {
            // Catches Bug #3 (Overdraft attempts)
            return BadRequest(new { status = 400, error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    [HttpPost("{id:guid}/disburse-partial-withdrawal")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> DisbursePartialWithdrawal(Guid id, [FromBody] DisbursePartialWithdrawalRequest request)
    {
        try
        {
            return Ok(await _claimService.DisbursePartialWithdrawalAsync(id, request));
        }
        catch (ArgumentException ex)
        {
            // Catches Bug #6 (Negative disbursements)
            return BadRequest(new { status = 400, error = ex.Message, timestamp = DateTime.UtcNow });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { status = 400, error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }
}