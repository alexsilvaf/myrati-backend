using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "BackofficeRead")]
[Route("api/v1/backoffice/profile")]
public sealed class ProfileController(IProfileService profileService) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileSnapshotDto>> Get(CancellationToken cancellationToken)
    {
        var response = await profileService.GetAsync(GetCurrentUserEmail(), cancellationToken);
        return Ok(response);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileInfoDto>> Update(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var response = await profileService.UpdateAsync(GetCurrentUserEmail(), request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await profileService.ChangePasswordAsync(GetCurrentUserEmail(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("sessions/{sessionId}/revoke")]
    public async Task<IActionResult> RevokeSession(string sessionId, CancellationToken cancellationToken)
    {
        await profileService.RevokeSessionAsync(GetCurrentUserEmail(), sessionId, cancellationToken);
        return NoContent();
    }
}
