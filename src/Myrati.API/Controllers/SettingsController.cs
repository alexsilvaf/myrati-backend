using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "BackofficeRead")]
[Route("api/v1/backoffice/settings")]
public sealed class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SettingsSnapshotDto>> Get(CancellationToken cancellationToken)
    {
        var response = await settingsService.GetAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPut]
    public async Task<ActionResult<SettingsSnapshotDto>> Update(
        [FromBody] UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost("api-keys")]
    public async Task<ActionResult<ApiKeyDto>> CreateApiKey(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await settingsService.CreateApiKeyAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost("api-keys/{apiKeyId}/rotate")]
    public async Task<ActionResult<ApiKeyDto>> RotateApiKey(string apiKeyId, CancellationToken cancellationToken)
    {
        var response = await settingsService.RotateApiKeyAsync(apiKeyId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost("api-keys/{apiKeyId}/toggle")]
    public async Task<ActionResult<ApiKeyDto>> ToggleApiKey(string apiKeyId, CancellationToken cancellationToken)
    {
        var response = await settingsService.ToggleApiKeyAsync(apiKeyId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpDelete("api-keys/{apiKeyId}")]
    public async Task<IActionResult> DeleteApiKey(string apiKeyId, CancellationToken cancellationToken)
    {
        await settingsService.DeleteApiKeyAsync(apiKeyId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost("team-members")]
    public async Task<ActionResult<TeamMemberDto>> CreateTeamMember(
        [FromBody] CreateTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        var response = await settingsService.CreateTeamMemberAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPut("team-members/{teamMemberId}")]
    public async Task<ActionResult<TeamMemberDto>> UpdateTeamMember(
        string teamMemberId,
        [FromBody] UpdateTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateTeamMemberAsync(teamMemberId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpDelete("team-members/{teamMemberId}")]
    public async Task<IActionResult> DeleteTeamMember(string teamMemberId, CancellationToken cancellationToken)
    {
        await settingsService.DeleteTeamMemberAsync(teamMemberId, cancellationToken);
        return NoContent();
    }
}
