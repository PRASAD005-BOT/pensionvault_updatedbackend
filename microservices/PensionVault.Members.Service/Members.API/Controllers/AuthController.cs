using Microsoft.AspNetCore.Mvc;
using Members.Services.DTOs;
using Members.Services;
using Members.Domain.Repositories;

namespace Members.API.Controllers;

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
        if (result.Success && result.Value != null)
        {
            SetAuthCookies(result.Value);
            return Ok(result.Value);
        }
        return StatusCode(result.StatusCode, new { message = result.Error, error = result.Error });
    }

    /// <summary>Register a new user account</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (result.Success && result.Value != null)
        {
            SetAuthCookies(result.Value);
            return CreatedAtAction(nameof(Login), result.Value);
        }
        return StatusCode(result.StatusCode, new { message = result.Error, error = result.Error });
    }

    /// <summary>Refresh an expired JWT using a refresh token</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest? request)
    {
        var tokenToUse = request?.RefreshToken;
        if (string.IsNullOrEmpty(tokenToUse))
        {
            Request.Cookies.TryGetValue("pv_refresh_token", out tokenToUse);
        }
        if (string.IsNullOrEmpty(tokenToUse))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var result = await _authService.RefreshTokenAsync(tokenToUse);
        if (result.Success && result.Value != null)
        {
            SetAuthCookies(result.Value);
            return Ok(result.Value);
        }
        return StatusCode(result.StatusCode, new { message = result.Error, error = result.Error });
    }

    /// <summary>Logout user and revoke HttpOnly cookies</summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("pv_token", new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = true, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax });
        Response.Cookies.Delete("pv_refresh_token", new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = true, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax });
        return Ok(new { message = "Logged out successfully" });
    }

    private void SetAuthCookies(AuthResponse authResponse)
    {
        if (!string.IsNullOrEmpty(authResponse.Token))
        {
            Response.Cookies.Append("pv_token", authResponse.Token, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true in HTTPS production
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = authResponse.Expiry != default ? authResponse.Expiry : System.DateTime.UtcNow.AddMinutes(60),
                Path = "/"
            });
        }

        if (!string.IsNullOrEmpty(authResponse.RefreshToken))
        {
            Response.Cookies.Append("pv_refresh_token", authResponse.RefreshToken, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true in HTTPS production
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = System.DateTime.UtcNow.AddDays(7),
                Path = "/"
            });
        }
    }

    /// <summary>Look up an employer organization by registration code/number</summary>
    [HttpGet("employer-lookup/{regNum}")]
    public async Task<IActionResult> LookupEmployer([FromServices] IEmployerRepository employerRepo, string regNum)
    {
        if (string.IsNullOrWhiteSpace(regNum) || regNum.Length < 4)
            return BadRequest(new { message = "Invalid lookup code. Must be at least 4 characters." });

        var all = await employerRepo.GetAllAsync();
        var emp = all.FirstOrDefault(e => {
            if (string.Equals(e.PortalJoinCode, regNum, System.StringComparison.OrdinalIgnoreCase))
                return true;
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

