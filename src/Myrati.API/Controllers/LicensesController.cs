using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "ProductScopedWrite")]
[Route("api/v1/backoffice")]
public sealed class LicensesController(IProductsService productsService) : ControllerBase
{
    [HttpPost("products/{productId}/licenses")]
    public async Task<ActionResult<LicenseDto>> CreateLicense(
        string productId,
        [FromBody] CreateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.CreateLicenseAsync(productId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("licenses/{licenseId}")]
    public async Task<ActionResult<LicenseDto>> UpdateLicense(
        string licenseId,
        [FromBody] UpdateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.UpdateLicenseAsync(licenseId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("licenses/{licenseId}/suspend")]
    public async Task<ActionResult<LicenseDto>> SuspendLicense(string licenseId, CancellationToken cancellationToken)
    {
        var response = await productsService.SuspendLicenseAsync(licenseId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("licenses/{licenseId}/reactivate")]
    public async Task<ActionResult<LicenseDto>> ReactivateLicense(string licenseId, CancellationToken cancellationToken)
    {
        var response = await productsService.ReactivateLicenseAsync(licenseId, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("licenses/{licenseId}")]
    public async Task<IActionResult> DeleteLicense(string licenseId, CancellationToken cancellationToken)
    {
        await productsService.DeleteLicenseAsync(licenseId, cancellationToken);
        return NoContent();
    }
}
