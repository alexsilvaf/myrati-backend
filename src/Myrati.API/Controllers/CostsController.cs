using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "BackofficeCostsRead")]
[Route("api/v1/backoffice/costs")]
public sealed class CostsController(ICostsService costsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CompanyCostDto>>> GetCosts(CancellationToken cancellationToken)
    {
        var response = await costsService.GetCostsAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost]
    public async Task<ActionResult<CompanyCostDto>> CreateCost(
        [FromBody] CreateCompanyCostRequest request,
        CancellationToken cancellationToken)
    {
        var response = await costsService.CreateCostAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPut("{costId}")]
    public async Task<ActionResult<CompanyCostDto>> UpdateCost(
        string costId,
        [FromBody] UpdateCompanyCostRequest request,
        CancellationToken cancellationToken)
    {
        var response = await costsService.UpdateCostAsync(costId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpDelete("{costId}")]
    public async Task<IActionResult> DeleteCost(string costId, CancellationToken cancellationToken)
    {
        await costsService.DeleteCostAsync(costId, cancellationToken);
        return NoContent();
    }
}
