using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Members.Services.DTOs;
using Members.Services;

namespace Members.API.Controllers;

[ApiController]
[Route("api/schemes")]
[Authorize]
[Produces("application/json")]
public class SchemesController : ControllerBase
{
    private readonly ISchemeService _schemeService;
    public SchemesController(ISchemeService schemeService)
        => _schemeService = schemeService;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
        => Ok(await _schemeService.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try { return Ok(await _schemeService.GetByIdAsync(id)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSchemeRequest request)
    {
        var result = await _schemeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.SchemeId }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSchemeRequest request)
    {
        try { return Ok(await _schemeService.UpdateAsync(id, request)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}

