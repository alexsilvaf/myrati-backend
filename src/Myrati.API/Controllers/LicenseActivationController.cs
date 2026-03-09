using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public")]
[Route("api/v1/public/licenses")]
public sealed class LicenseActivationController(ILicenseActivationService licenseActivationService) : ControllerBase
{
    [HttpPost("activate")]
    public async Task<ActionResult<LicenseActivationResponse>> Activate(
        [FromBody] LicenseActivationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await licenseActivationService.ActivateAsync(request, cancellationToken);
        return Ok(response);
    }
}
