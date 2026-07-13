using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Members.Data;
using Members.Domain.Entities;
using PensionVault.Shared.Contracts;

namespace Members.API.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly MembersDbContext _context;

    public AuditController(MembersDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AuditEventRequest request)
    {
        var log = new AuditLog
        {
            AuditId = Guid.NewGuid(),
            UserId = request.UserId,
            Action = request.Action,
            EntityType = request.EntityType,
            RecordId = request.RecordId,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
        return Ok();
    }
}




