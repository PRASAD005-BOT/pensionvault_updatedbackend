using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionVault.Application.DTOs.Investment;
using PensionVault.Application.Interfaces;

namespace PensionVault.Contributions.Service.Controllers;

[ApiController]
[Route("api/portfolios")]
[Authorize(Roles = "InvestmentOfficer,FundAdmin,Admin,Compliance")]
[Produces("application/json")]
public class PortfoliosController : ControllerBase
{
    private readonly IInvestmentService _investmentService;
    public PortfoliosController(IInvestmentService investmentService)
        => _investmentService = investmentService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? schemeId)
        => Ok(await _investmentService.GetPortfoliosAsync(schemeId));

    [HttpPost]
    [Authorize(Roles = "InvestmentOfficer,Admin")]
    public async Task<IActionResult> Create([FromBody] CreatePortfolioRequest request)
    {
        var result = await _investmentService.CreatePortfolioAsync(request);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "InvestmentOfficer,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePortfolioRequest request)
        => Ok(await _investmentService.UpdatePortfolioAsync(id, request));
}

