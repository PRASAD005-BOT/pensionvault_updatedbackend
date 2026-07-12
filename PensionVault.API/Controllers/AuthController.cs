using Microsoft.AspNetCore.Mvc;
using PensionVault.Application.DTOs.Auth;
using PensionVault.Application.Interfaces;

namespace PensionVault.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Login and receive a JWT token</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>Register a new user account</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(Login), result);
    }

    /// <summary>Refresh an expired JWT using a refresh token</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return Ok(result);
    }

    /// <summary>Look up an employer organization by registration code/number</summary>
    [HttpGet("employer-lookup/{regNum}")]
    public async Task<IActionResult> LookupEmployer([FromServices] PensionVault.Domain.Interfaces.IEmployerRepository employerRepo, string regNum)
    {
        if (string.IsNullOrWhiteSpace(regNum) || regNum.Length < 4)
            return BadRequest(new { message = "Invalid lookup code. Must be at least 4 characters." });

        var all = await employerRepo.GetAllAsync();
        var emp = all.FirstOrDefault(e => {
            // 1. Check JSON property
            if (!string.IsNullOrEmpty(e.ContactDetails)) {
                try {
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(e.ContactDetails);
                    if (jsonDoc.RootElement.TryGetProperty("portalJoinCode", out var codeProp)) {
                        var codeVal = codeProp.GetString();
                        if (string.Equals(codeVal, regNum, System.StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                } catch { }
            }
            // 2. Check deterministic fallback based on EmployerId
            var fallback = GetFallbackCode(e.EmployerId);
            return string.Equals(fallback, regNum, System.StringComparison.OrdinalIgnoreCase);
        });

        if (emp == null)
            return NotFound(new { message = "No registered employer matches this code." });

        return Ok(new {
            employerId = emp.EmployerId,
            companyName = emp.CompanyName,
            registrationNumber = emp.RegistrationNumber,
            industry = emp.Industry
        });
    }

    private string GetFallbackCode(Guid guid)
    {
        var guidStr = guid.ToString();
        int sum = 0;
        foreach (var c in guidStr)
        {
            sum += (int)c;
        }
        return (100000 + (sum % 900000)).ToString();
    }
}
