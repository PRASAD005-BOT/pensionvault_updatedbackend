using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PensionVault.Infrastructure.Data;

namespace PensionVault.Members.Service.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Compliance,Admin,FundAdmin,InvestmentOfficer")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly MembersDbContext _context;

    public ReportsController(MembersDbContext context)
    {
        _context = context;
    }

    /// <summary>Get audit trail with optional filters</summary>
    [HttpGet("audit-trail")]
    public async Task<IActionResult> AuditTrail(
        [FromQuery] string? entityType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _context.AuditLogs.Include(a => a.User).AsQueryable();
        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to);

        var results = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(1000)
            .Select(a => new
            {
                a.AuditId, UserName = a.User.Name,
                a.Action, a.EntityType, a.RecordId, a.Timestamp
            })
            .ToListAsync();
        return Ok(results);
    }
}

