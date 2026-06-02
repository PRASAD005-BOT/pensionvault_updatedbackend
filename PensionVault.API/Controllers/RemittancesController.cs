using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionVault.Application.DTOs.Contributions;
using PensionVault.Application.Services;

namespace PensionVault.API.Controllers;

[ApiController]
[Route("api/remittances")]
[Authorize]
[Produces("application/json")]
public class RemittancesController : ControllerBase
{
    private readonly IContributionService _contributionService;
    public RemittancesController(IContributionService contributionService)
        => _contributionService = contributionService;

    /// <summary>Submit a contribution remittance for an employer</summary>
    [HttpPost]
    [Authorize(Roles = "Employer,FundAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateRemittanceRequest request)
    {
        var result = await _contributionService.CreateRemittanceAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.RemittanceId }, result);
    }

    /// <summary>Get remittance details</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Employer,FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _contributionService.GetRemittanceAsync(id));

    /// <summary>Reconcile a remittance against enrolled headcount</summary>
    [HttpPost("{id:guid}/reconcile")]
    [Authorize(Roles = "FundAdmin,Admin")]
    public async Task<IActionResult> Reconcile(Guid id)
        => Ok(await _contributionService.ReconcileAsync(id));

    /// <summary>Get detailed reconciliation report for a remittance</summary>
    [HttpGet("{id:guid}/reconciliation-report")]
    [Authorize(Roles = "FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetReconciliationReport(Guid id)
        => Ok(await _contributionService.GetReconciliationReportAsync(id));

    /// <summary>Get all remittances with default status</summary>
    [HttpGet("defaulters")]
    [Authorize(Roles = "FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetDefaulters()
        => Ok(await _contributionService.GetDefaulterRemittancesAsync());

    /// <summary>Get overdue remittances beyond specified days threshold</summary>
    [HttpGet("overdue")]
    [Authorize(Roles = "FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetOverdue([FromQuery] int delayDays = 30)
        => Ok(await _contributionService.GetOverdueRemittancesAsync(delayDays));

    /// <summary>Get defaulter summary for specific employer</summary>
    [HttpGet("employer/{employerId:guid}/defaulter-summary")]
    [Authorize(Roles = "FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetDefaulterSummary(Guid employerId)
        => Ok(await _contributionService.GetDefaulterSummaryAsync(employerId));

    /// <summary>Get all contributions for a specific member</summary>
    [HttpGet("member/{memberId:guid}")]
    [Authorize(Roles = "Member,FundAdmin,Admin")]
    public async Task<IActionResult> GetMemberContributions(Guid memberId)
        => Ok(await _contributionService.GetMemberContributionsAsync(memberId));
}
