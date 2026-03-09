using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "BackofficeRead")]
[Route("api/v1/backoffice/users")]
public sealed class UsersController(IUsersService usersService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserDirectoryItemDto>>> GetUsers(
        [FromQuery] UserDirectoryQuery query,
        CancellationToken cancellationToken)
    {
        var response = await usersService.GetUsersAsync(query, cancellationToken);
        return Ok(response);
    }
}
