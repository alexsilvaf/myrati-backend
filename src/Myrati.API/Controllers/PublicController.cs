using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public")]
[Route("api/v1/public")]
public sealed class PublicController(IPublicSiteService publicSiteService) : ControllerBase
{
    [HttpPost("contact")]
    public async Task<ActionResult<ContactResponse>> SubmitContact(
        [FromBody] ContactRequest request,
        CancellationToken cancellationToken)
    {
        var response = await publicSiteService.SubmitContactAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("status")]
    public async Task<ActionResult<SystemStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var response = await publicSiteService.GetSystemStatusAsync(cancellationToken);
        return Ok(response);
    }
}
