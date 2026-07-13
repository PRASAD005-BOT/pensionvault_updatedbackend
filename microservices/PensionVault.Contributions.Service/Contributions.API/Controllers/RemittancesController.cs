using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Contributions.Services.DTOs;
using Contributions.Services;
using Contributions.Services.HttpClients;

namespace Contributions.API.Controllers;

[ApiController]
[Route("api/remittances")]
[Authorize]
[Produces("application/json")]
public class RemittancesController : ControllerBase
{
    private readonly IContributionService _contributionService;
    public RemittancesController(IContributionService contributionService)
        => _contributionService = contributionService;

    [HttpGet]
    [Authorize(Roles = "Employer,FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? employerId)
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId)) 
                return Ok(new List<RemittanceResponse>());
            return Ok(await _contributionService.GetEmployerRemittancesAsync(orgId));
        }
        if (employerId.HasValue)
        {
            return Ok(await _contributionService.GetEmployerRemittancesAsync(employerId.Value));
        }
        return Ok(await _contributionService.GetAllRemittancesAsync());
    }

    [HttpPost]
    [Authorize(Roles = "Employer,FundAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateRemittanceRequest request)
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim != null && Guid.TryParse(orgClaim.Value, out var orgId))
            {
                request = request with { EmployerId = orgId };
            }
        }
        var result = await _contributionService.CreateRemittanceAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.RemittanceId }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Employer,FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _contributionService.GetRemittanceAsync(id));

    [HttpPost("{id:guid}/reconcile")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Reconcile(Guid id)
        => Ok(await _contributionService.ReconcileAsync(id));

    [HttpGet("member/{memberId:guid}")]
    [Authorize(Roles = "Member,FundAdmin,Admin,Employer")]
    public async Task<IActionResult> GetMemberContributions(Guid memberId, [FromServices] MemberServiceClient memberClient)
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId)) return Forbid();
            var member = await memberClient.GetMemberByIdAsync(memberId);
            if (member == null || member.EmployerId != orgId) return Forbid();
        }
        else if (User.IsInRole("Member"))
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();
            var member = await memberClient.GetMemberByUserIdAsync(userId);
            if (member == null || member.MemberId != memberId) return Forbid();
        }
        return Ok(await _contributionService.GetMemberContributionsAsync(memberId));
    }

    [HttpGet("{id:guid}/reconciliation-report")]
    [Authorize(Roles = "FundAdmin,Admin,Employer")]
    public async Task<IActionResult> GetReconciliationReport(Guid id)
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId)) return Forbid();
            var remittance = await _contributionService.GetRemittanceAsync(id);
            if (remittance.EmployerId != orgId) return Forbid();
        }
        return Ok(await _contributionService.GetReconciliationReportAsync(id));
    }

    [HttpGet("defaulters")]
    [Authorize(Roles = "FundAdmin,Admin,Compliance,Employer")]
    public async Task<IActionResult> GetDefaulters()
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId)) return Forbid();
            var allDefaulters = await _contributionService.GetDefaultersAsync();
            return Ok(allDefaulters.Where(d => d.EmployerId == orgId));
        }
        return Ok(await _contributionService.GetDefaultersAsync());
    }

    [HttpGet("overdue")]
    [Authorize(Roles = "FundAdmin,Admin,Compliance,Employer")]
    public async Task<IActionResult> GetOverdueRemittances()
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId)) return Forbid();
            var allOverdue = await _contributionService.GetOverdueRemittancesAsync();
            return Ok(allOverdue.Where(o => o.EmployerId == orgId));
        }
        return Ok(await _contributionService.GetOverdueRemittancesAsync());
    }

    [HttpGet("employer/{employerId:guid}/defaulter-summary")]
    [Authorize(Roles = "FundAdmin,Admin,Compliance,Employer")]
    public async Task<IActionResult> GetDefaulterSummary(Guid employerId)
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId) || orgId != employerId) 
                return Forbid();
        }
        return Ok(await _contributionService.GetDefaulterSummaryAsync(employerId));
    }
}

