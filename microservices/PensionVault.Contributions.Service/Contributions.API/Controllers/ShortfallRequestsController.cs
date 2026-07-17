using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Contributions.Services.DTOs;
using Contributions.Services;
using Contributions.Services.HttpClients;

namespace Contributions.API.Controllers;

[ApiController]
[Route("api/shortfall-requests")]
[Authorize]
[Produces("application/json")]
public class ShortfallRequestsController : ControllerBase
{
    private readonly IContributionService _contributionService;
    private readonly MemberServiceClient _memberClient;

    public ShortfallRequestsController(IContributionService contributionService, MemberServiceClient memberClient)
    {
        _contributionService = contributionService;
        _memberClient = memberClient;
    }

    /// <summary>Member raises a shortfall issue on one of their own posted contributions</summary>
    [HttpPost]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Raise([FromBody] CreateShortfallRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();
        var member = await _memberClient.GetMemberByUserIdAsync(userId);
        if (member == null) return Forbid();
        var result = await _contributionService.RaiseShortfallAsync(member.MemberId, request);
        return Ok(result);
    }

    /// <summary>List shortfall requests — Admin/FundAdmin see all, Employer sees their own organisation's</summary>
    [HttpGet]
    [Authorize(Roles = "FundAdmin,Admin,Employer,Compliance")]
    public async Task<IActionResult> GetAll()
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId))
                return Ok(Array.Empty<ShortfallRequestResponse>());
            return Ok(await _contributionService.GetShortfallRequestsByEmployerAsync(orgId));
        }
        return Ok(await _contributionService.GetAllShortfallRequestsAsync());
    }

    /// <summary>Member views their own raised shortfall requests</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> GetMine()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();
        var member = await _memberClient.GetMemberByUserIdAsync(userId);
        if (member == null) return Ok(Array.Empty<ShortfallRequestResponse>());
        return Ok(await _contributionService.GetShortfallRequestsByMemberAsync(member.MemberId));
    }

    /// <summary>Employer/FundAdmin/Admin corrects the transaction amount and marks the shortfall resolved</summary>
    [HttpPut("{id:guid}/resolve")]
    [Authorize(Roles = "FundAdmin,Admin,Employer")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveShortfallRequest request)
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId)) return Forbid();
            var mine = await _contributionService.GetShortfallRequestsByEmployerAsync(orgId);
            if (!mine.Any(s => s.ShortfallRequestId == id)) return Forbid();
        }
        return Ok(await _contributionService.ResolveShortfallAsync(id, request));
    }

    /// <summary>Employer/FundAdmin/Admin rejects a raised shortfall request</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "FundAdmin,Admin,Employer")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectShortfallRequest request)
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId)) return Forbid();
            var mine = await _contributionService.GetShortfallRequestsByEmployerAsync(orgId);
            if (!mine.Any(s => s.ShortfallRequestId == id)) return Forbid();
        }
        return Ok(await _contributionService.RejectShortfallAsync(id, request));
    }
}
