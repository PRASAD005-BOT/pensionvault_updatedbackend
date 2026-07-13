using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Contributions.Services.DTOs;
using Contributions.Services;

namespace Contributions.API.Controllers;

[ApiController]
[Route("api/corpus")]
[Authorize(Roles = "InvestmentOfficer,FundAdmin,Admin,Compliance")]
[Produces("application/json")]
public class CorpusController : ControllerBase
{
    private readonly IInvestmentService _investmentService;
    public CorpusController(IInvestmentService investmentService)
        => _investmentService = investmentService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? schemeId)
        => Ok(await _investmentService.GetCorpusRecordsAsync(schemeId));

    [HttpPost]
    [Authorize(Roles = "InvestmentOfficer,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCorpusRequest request)
    {
        var result = await _investmentService.CreateCorpusRecordAsync(request);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpPost("{id:guid}/finalise")]
    [Authorize(Roles = "InvestmentOfficer,Admin")]
    public async Task<IActionResult> Finalise(Guid id)
        => Ok(await _investmentService.FinaliseCorpusAsync(id));
}

