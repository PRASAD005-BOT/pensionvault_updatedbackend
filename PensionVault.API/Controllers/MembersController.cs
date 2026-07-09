using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionVault.Application.DTOs.Members;
using PensionVault.Application.Interfaces;

namespace PensionVault.API.Controllers;

[ApiController]
[Route("api/members")]
[Authorize]
[Produces("application/json")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    public MembersController(IMemberService memberService) => _memberService = memberService;

    /// <summary>Get all members (FundAdmin only)</summary>
    [HttpGet]
    [Authorize(Roles = "Member,Employer,FundAdmin,Admin")]
    public async Task<IActionResult> GetAll()
    {
        if (User.IsInRole("Member"))
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();
            try
            {
                var member = await _memberService.GetByUserIdAsync(userId);
                return Ok(new List<MemberResponse> { member });
            }
            catch { return Ok(new List<MemberResponse>()); }
        }

        Guid? employerId = null;
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var parsedOrgId))
                return Ok(new List<MemberResponse>());
            employerId = parsedOrgId;
        }
        return Ok(await _memberService.GetAllAsync(employerId));
    }

    /// <summary>Get a specific member by ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var member = await _memberService.GetByIdAsync(id);
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId) || member.EmployerId != orgId)
                return Forbid();
        }
        return Ok(member);
    }

    /// <summary>Get the authenticated member's profile</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Member,FundAdmin,Admin")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();
        return Ok(await _memberService.GetByUserIdAsync(userId));
    }

    /// <summary>Enrol a new member</summary>
    [HttpPost]
    [Authorize(Roles = "Employer,FundAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMemberRequest request)
    {
        // 1. Structural Validation Check (Intercepts Empty Strings & Invalid Regex Formats)
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 2. Age Rule Check: Enforce minimum 20 years difference between DOB and Joining Date
        var minimumAllowedJoiningDate = request.DateOfBirth.AddYears(20);
        if (request.JoiningDate < minimumAllowedJoiningDate)
        {
            return BadRequest(new { Error = "Invalid chronological timeline. The difference between Date of Birth and Joining Date must be at least 20 years." });
        }

        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId)) return Forbid();
            request = request with { EmployerId = orgId };
        }

        // 3. Automated Rule Integration: Set retirement date implicitly to exactly age 60
        var computedRetirementDate = request.DateOfBirth.AddYears(60);

        // We instantiate a modified payload configuration matching your business specifications
        // Note: If your IMemberService interface expects DateOfRetirement inside CreateMemberRequest, 
        // we can dynamically assign it using standard C# record mutation notation below:
        var updatedRequest = request with { DateOfRetirement = computedRetirementDate };

        var result = await _memberService.CreateAsync(updatedRequest);
        return CreatedAtAction(nameof(GetById), new { id = result.MemberId }, result);
    }


    /// <summary>Update member details</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Member,Employer,FundAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMemberRequest request)
    {
        // 1. Intercept structural model errors (empty strings, bad national ID formats)
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 2. Fetch the existing member profile from the database to find their immutable DateOfBirth
        var existingMember = await _memberService.GetByIdAsync(id);
        if (existingMember == null)
        {
            return NotFound(new { Error = "Member profile not found." });
        }

        // 3. Enforce the 60-year retirement calculation rule automatically!
        var computedRetirementDate = existingMember.DateOfBirth.AddYears(60);

        // We mutate the incoming request using C# record 'with' expression to force the correct date
        var sanitizedRequest = request with { DateOfRetirement = computedRetirementDate };

        // 4. Role-based security validation checks
        if (User.IsInRole("Member"))
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

            var member = await _memberService.GetByUserIdAsync(userId);
            if (member.MemberId != id) return Forbid();

            // Protect status modification for regular member users
            sanitizedRequest = sanitizedRequest with { Status = member.Status };
        }
        else if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId) || existingMember.EmployerId != orgId)
            {
                return Forbid();
            }
        }

        // 5. Pass the strictly sanitized request containing the auto-computed date to the database
        var result = await _memberService.UpdateAsync(id, sanitizedRequest);
        return Ok(result);
    }

    /// <summary>Self-enroll a member profile</summary>
    [HttpPost("self-enroll")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> SelfEnroll([FromBody] SelfEnrollMemberRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();
        var result = await _memberService.SelfEnrollAsync(userId, request);
        return Ok(result);
    }

    /// <summary>Approve a pending member profile</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Employer,FundAdmin,Admin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveMemberRequest request)
    {
        if (User.IsInRole("Employer"))
        {
            var member = await _memberService.GetByIdAsync(id);
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId) || member.EmployerId != orgId || request.EmployerId != orgId)
                return Forbid();
        }
        return Ok(await _memberService.ApproveAsync(id, request));
    }

    /// <summary>Get a member's fund accounts</summary>
    [HttpGet("{id:guid}/fund-accounts")]
    public async Task<IActionResult> GetFundAccounts(Guid id)
        => Ok(await _memberService.GetFundAccountsAsync(id));

    /// <summary>Get a member's contribution history</summary>
    [HttpGet("{id:guid}/contributions")]
    public async Task<IActionResult> GetContributions(Guid id)
        => Ok(await _memberService.GetContributionsAsync(id));

    /// <summary>Get a member's ledger entries</summary>
    [HttpGet("{id:guid}/ledger")]
    public async Task<IActionResult> GetLedger(Guid id)
        => Ok(await _memberService.GetLedgerAsync(id));

    /// <summary>Get a member's benefit claims</summary>
    [HttpGet("{id:guid}/claims")]
    public async Task<IActionResult> GetClaims(Guid id)
        => Ok(await _memberService.GetClaimsAsync(id));
}