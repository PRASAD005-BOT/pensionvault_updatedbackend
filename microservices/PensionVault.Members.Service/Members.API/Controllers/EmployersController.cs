using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Members.Services.DTOs;
using Members.Services;
using Members.Services.ProxyServices;
using Members.Domain.Entities;

namespace Members.API.Controllers;

[ApiController]
[Route("api/employers")]
[Authorize]
[Produces("application/json")]
public class EmployersController : ControllerBase
{
    private readonly IEmployerService _employerService;
    private readonly ContributionsServiceClient _contributionsClient;

    public EmployersController(IEmployerService employerService, ContributionsServiceClient contributionsClient)
    {
        _employerService = employerService;
        _contributionsClient = contributionsClient;
    }

    /// <summary>
    /// Get all employers. Defaults to activeOnly=false so management pages can render deactivated companies.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Member,FundAdmin,Admin,Compliance,Employer")]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var employers = await _employerService.GetAllAsync();

        if (activeOnly)
        {
            // Filter out deactivated/inactive employers based on Status string when explicitly requested
            employers = employers.Where(e =>
                string.Equals(e.Status, "Active", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.Status, "0", StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        return Ok(employers);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Member,Employer,FundAdmin,Admin,Compliance")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _employerService.GetByIdAsync(id));

    [HttpGet("me")]
    [Authorize(Roles = "Employer,FundAdmin,Admin")]
    public async Task<IActionResult> GetMyEmployer()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();
        return Ok(await _employerService.GetByUserIdAsync(userId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,FundAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateEmployerRequest request)
    {
        var result = await _employerService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.EmployerId }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Employer,FundAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployerRequest request)
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId) || orgId != id)
                return Forbid();

            request = request with { Status = null };
        }

        return Ok(await _employerService.UpdateAsync(id, request));
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Admin,FundAdmin")]
    public async Task<IActionResult> Approve(Guid id) => Ok(await _employerService.ApproveAsync(id));

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Admin,FundAdmin")]
    public async Task<IActionResult> Reject(Guid id) => Ok(await _employerService.RejectAsync(id));

    [HttpGet("{id:guid}/remittances")]
    [Authorize(Roles = "Employer,FundAdmin,Admin")]
    public async Task<IActionResult> GetRemittances(Guid id)
        => Ok(await _contributionsClient.GetEmployerRemittancesAsync(id));
}