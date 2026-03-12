using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : AuthenticatedControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("public")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("public")]
    [HttpGet("password-setup")]
    public async Task<ActionResult<PasswordSetupSessionDto>> GetPasswordSetupSession(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var response = await authService.GetPasswordSetupSessionAsync(token, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("public")]
    [HttpPost("password-setup")]
    public async Task<IActionResult> CompletePasswordSetup(
        [FromBody] PasswordSetupRequest request,
        CancellationToken cancellationToken)
    {
        await authService.CompletePasswordSetupAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "BackofficeRead")]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
    {
        var response = await authService.GetCurrentUserAsync(GetCurrentUserEmail(), cancellationToken);
        return Ok(response);
    }
}
