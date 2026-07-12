using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionVault.Application.DTOs.Annuity;
using PensionVault.Application.Interfaces;

namespace PensionVault.Annuity.Service.Controllers;

[ApiController]
[Route("api/annuity")]
[Authorize]
[Produces("application/json")]
public class AnnuityController : ControllerBase
{
    private readonly IAnnuityService _annuityService;
    private readonly IMemberService _memberService;
    public AnnuityController(IAnnuityService annuityService, IMemberService memberService)
    {
        _annuityService = annuityService;
        _memberService = memberService;
    }

    [HttpGet]
    [Authorize(Roles = "FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetAll()
        => Ok(await _annuityService.GetAllAnnuitiesAsync());

    [HttpPost]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAnnuityRequest request)
    {
        var result = await _annuityService.CreateAnnuityAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.AnnuityId }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _annuityService.GetAnnuityAsync(id));

    [HttpGet("{id:guid}/disbursements")]
    public async Task<IActionResult> GetDisbursements(Guid id)
        => Ok(await _annuityService.GetDisbursementsAsync(id));

    [HttpPost("{id:guid}/disburse")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> ProcessDisbursement(Guid id, [FromBody] ProcessDisbursementRequest request)
    {
        var req = request with { AnnuityId = id };
        return Ok(await _annuityService.ProcessDisbursementAsync(req));
    }

    [HttpPost("{id:guid}/nominee-settlement")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> ProcessNomineeSettlement(Guid id, [FromBody] NomineeSettlementRequest request)
        => Ok(await _annuityService.ProcessNomineeSettlementAsync(id, request));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> UpdateAnnuity(Guid id, [FromBody] UpdateAnnuityRequest request)
        => Ok(await _annuityService.UpdateAnnuityAsync(id, request));

    [HttpPut("{id:guid}/terminate")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> TerminateAnnuity(Guid id)
        => Ok(await _annuityService.TerminateAnnuityAsync(id));

    [HttpPost("requests")]
    [Authorize(Roles = "Member,FundAdmin,Admin")]
    public async Task<IActionResult> SubmitRequest([FromBody] SubmitAnnuityRequestDto dto)
    {
        if (User.IsInRole("Member"))
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
            try {
                var member = await _memberService.GetByUserIdAsync(userId);
                dto = dto with { MemberId = member.MemberId };
            } catch { return BadRequest("Could not determine member identity from token."); }
        }
        var result = await _annuityService.SubmitAnnuityRequestAsync(dto);
        return Ok(result);
    }

    [HttpGet("requests")]
    [Authorize(Roles = "FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetPendingRequests()
        => Ok(await _annuityService.GetPendingRequestsAsync());

    [HttpGet("requests/my")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
        try {
            var member = await _memberService.GetByUserIdAsync(userId);
            return Ok(await _annuityService.GetMemberRequestsAsync(member.MemberId));
        } catch { return BadRequest("Could not determine member identity from token."); }
    }

    [HttpPut("requests/{requestId:guid}/approve")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> ApproveRequest(Guid requestId)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? Guid.Empty.ToString();
        Guid.TryParse(userIdStr, out var reviewerUserId);
        return Ok(await _annuityService.ApproveRequestAsync(requestId, reviewerUserId));
    }

    [HttpPut("requests/{requestId:guid}/reject")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> RejectRequest(Guid requestId, [FromBody] RejectRequestDto body)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? Guid.Empty.ToString();
        Guid.TryParse(userIdStr, out var reviewerUserId);
        return Ok(await _annuityService.RejectRequestAsync(requestId, reviewerUserId, body.ReviewNote));
    }

    [HttpPut("requests/{requestId:guid}/cancel")]
    [Authorize(Roles = "Member,FundAdmin,Admin")]
    public async Task<IActionResult> CancelRequest(Guid requestId)
    {
        Guid memberId = Guid.Empty;
        if (User.IsInRole("Member")) {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
            try {
                var member = await _memberService.GetByUserIdAsync(userId);
                memberId = member.MemberId;
            } catch { return BadRequest("Could not determine member identity."); }
        }
        return Ok(await _annuityService.CancelRequestAsync(requestId, memberId));
    }

    [HttpGet("eligibility/{memberId:guid}")]
    [Authorize(Roles = "Member,FundAdmin,Admin")]
    public async Task<IActionResult> CheckEligibility(Guid memberId)
    {
        if (User.IsInRole("Member"))
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
            try {
                var member = await _memberService.GetByUserIdAsync(userId);
                if (member.MemberId != memberId) return Forbid();
            } catch { return Forbid(); }
        }
        return Ok(await _annuityService.CheckEligibilityAsync(memberId));
    }
}

public record RejectRequestDto(string? ReviewNote);

