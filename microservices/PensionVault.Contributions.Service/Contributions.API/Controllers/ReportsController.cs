using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contributions.Data;
using Contributions.Domain.Entities;
using Contributions.Services;

namespace Contributions.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Compliance,Admin,FundAdmin,InvestmentOfficer")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ContributionsDbContext _context;

    public ReportsController(IReportService reportService, ContributionsDbContext context)
    {
        _reportService = reportService;
        _context = context;
    }

    /// <summary>Get employers who are in default or shortfall status</summary>
    [HttpGet("contribution-defaults")]
    public async Task<IActionResult> ContributionDefaults()
        => Ok(await _reportService.GetContributionDefaultsAsync());

    /// <summary>Statutory returns — contribution summary by period</summary>
    [HttpGet("statutory-returns")]
    public async Task<IActionResult> StatutoryReturns([FromQuery] string? period)
        => Ok(await _reportService.GetStatutoryReturnsAsync(period));

    [HttpPost("fix-data")]
    [AllowAnonymous]
    public async Task<IActionResult> FixData()
    {
        var schemes = await _context.FundSchemes.ToListAsync();
        foreach (var s in schemes)
        {
            if (s.SchemeName.Contains("EPF") || s.SchemeType == SchemeType.EPF)
            {
                s.EmployeeContributionRate = 12;
                s.EmployerContributionRate = 12;
                s.InterestRatePA = 8.25m;
                s.Status = SchemeStatus.Active;
            }
            else
            {
                s.EmployerContributionRate = 4.81m;
                s.InterestRatePA = 7.5m;
                s.Status = SchemeStatus.Active;
            }
        }
        await _context.SaveChangesAsync();
        return Ok("Fixed");
    }
}

