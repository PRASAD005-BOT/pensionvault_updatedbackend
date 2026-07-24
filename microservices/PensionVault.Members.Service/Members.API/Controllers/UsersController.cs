using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Members.Services;
using Members.Domain.Repositories;
using Members.Data;
using Members.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Members.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _env;

    public UsersController(IUserService userService, IWebHostEnvironment env)
    {
        _userService = userService;
        _env = env;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,FundAdmin")]
    public async Task<IActionResult> GetAll([FromServices] MembersDbContext context)
    {
        var users = await context.Users.Include(u => u.Member).ToListAsync();
        return Ok(users.Select(u => new {
            userId = u.UserId,
            name = u.Name,
            email = u.Email,
            role = u.Role.ToString(),
            status = u.Status.ToString(),
            organisationId = u.OrganisationId ?? u.Member?.EmployerId,
            employeeId = u.EmployeeId ?? u.Member?.MembershipNumber
        }));
    }

    [HttpPost("me/image")]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        var uploadsFolder = Path.Combine(
            _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            "uploads", "profiles");
        Directory.CreateDirectory(uploadsFolder);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{userId}_{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var fileUrl = await _userService.UploadProfileImageAsync(userId, fileName, Stream.Null, ext);
        return Ok(new { ProfileImageUrl = fileUrl });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        await _userService.UpdateProfileAsync(userId, request.Name, request.Phone);
        return Ok(new { request.Name, request.Phone });
    }

    /// <summary>Get list of registered users (role=Member) who are not yet enrolled as members</summary>
    [HttpGet("unenrolled-members")]
    [Authorize(Roles = "Admin,FundAdmin,Employer")]
    public async Task<IActionResult> GetUnenrolledMembers(
        [FromServices] IUserRepository userRepo, 
        [FromServices] IMemberRepository memberRepo)
    {
        var users = await userRepo.GetByRoleAsync(UserRole.Member);
        var members = await memberRepo.GetAllAsync();
        var enrolledUserIds = members.Select(m => m.UserId).ToHashSet();

        var unenrolled = users.Where(u => !enrolledUserIds.Contains(u.UserId))
            .Select(u => new {
                userId = u.UserId,
                name = u.Name,
                email = u.Email,
                phone = u.Phone
            })
            .ToList();

        return Ok(unenrolled);
    }

    /// <summary>Get registered employer representative users linked to a specific employer</summary>
    [HttpGet("employer-representatives/{employerId:guid}")]
    [Authorize(Roles = "Admin,FundAdmin,Employer")]
    public async Task<IActionResult> GetEmployerRepresentatives(Guid employerId, [FromServices] IUserRepository userRepo)
    {
        if (User.IsInRole("Employer"))
        {
            var orgClaim = User.FindFirst("OrganisationId");
            if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var orgId) || orgId != employerId)
                return Forbid();
        }
        var users = await userRepo.GetByOrgAndRoleAsync(employerId, UserRole.Employer);
        var reps = users.Select(u => new {
            userId = u.UserId,
            name = u.Name,
            email = u.Email,
            phone = u.Phone
        }).ToList();

        return Ok(reps);
    }

    /// <summary>Change profile password with ID verification</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromServices] IMemberRepository memberRepo,
        [FromServices] IEmployerRepository employerRepo,
        [FromServices] IUserRepository userRepo,
        [FromServices] IUnitOfWork unitOfWork,
        [FromBody] ChangePasswordRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        var user = await userRepo.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        if (user.Role == UserRole.Member)
        {
            var member = await memberRepo.FindByUserIdAsync(userId);
            if (member == null) return BadRequest("Member profile not found.");

            if (!string.Equals(request.VerificationCode, member.MembershipNumber, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid Membership Number. Verification failed.");
        }
        else if (user.Role == UserRole.Employer)
        {
            if (!user.OrganisationId.HasValue) return BadRequest("Employer profile not found.");
            var employer = await employerRepo.FindByIdAsync(user.OrganisationId.Value);
            if (employer == null) return BadRequest("Employer profile not found.");

            string portalCode = "";
            if (!string.IsNullOrEmpty(employer.ContactDetails))
            {
                try
                {
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(employer.ContactDetails);
                    if (jsonDoc.RootElement.TryGetProperty("portalJoinCode", out var codeProp))
                    {
                        portalCode = codeProp.GetString() ?? "";
                    }
                }
                catch { }
            }
            var guidStr = employer.EmployerId.ToString();
            int sum = 0;
            foreach (var c in guidStr) sum += (int)c;
            string fallbackCode = (100000 + (sum % 900000)).ToString();

            bool verified = (!string.IsNullOrEmpty(portalCode) && string.Equals(request.VerificationCode, portalCode, StringComparison.OrdinalIgnoreCase)) ||
                             string.Equals(request.VerificationCode, fallbackCode, StringComparison.OrdinalIgnoreCase);

            if (!verified) return BadRequest("Invalid Portal Join Code. Verification failed.");
        }
        else
        {
            if (!string.Equals(request.VerificationCode, user.Email, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Verification code (email) does not match.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest("Password must be at least 6 characters long.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully!" });
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(Guid id, [FromServices] IUserRepository userRepo)
    {
        var user = await userRepo.FindByIdAsync(id);
        if (user == null) return NotFound("User not found.");
        return Ok(new {
            userId = user.UserId,
            name = user.Name,
            email = user.Email,
            role = user.Role.ToString()
        });
    }

    [HttpGet("by-role/{role}")]
    [Authorize]
    public async Task<IActionResult> GetByRole(string role, [FromServices] IUserRepository userRepo)
    {
        if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            return BadRequest("Invalid role.");
        var users = await userRepo.GetByRoleAsync(parsedRole);
        return Ok(users.Select(u => new {
            userId = u.UserId,
            name = u.Name,
            email = u.Email,
            role = u.Role.ToString()
        }));
    }
}

public record UpdateUserRequest(string Name, string? Phone);
public record ChangePasswordRequest(string VerificationCode, string NewPassword);


