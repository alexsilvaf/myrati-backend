using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "PortalRead")]
[Route("api/v1/portal")]
public sealed class PortalController(IPortalService portalService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<PortalMeDto>> GetMe(CancellationToken cancellationToken)
    {
        var response = await portalService.GetPortalMeAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("licenses/{licenseId}/users")]
    public async Task<ActionResult<IReadOnlyCollection<UserDirectoryItemDto>>> GetLicenseUsers(
        string licenseId,
        CancellationToken cancellationToken)
    {
        var response = await portalService.GetLicenseUsersAsync(licenseId, cancellationToken);
        return Ok(response);
    }
}
