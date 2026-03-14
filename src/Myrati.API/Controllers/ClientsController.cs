using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "BackofficeRead")]
[Route("api/v1/backoffice/clients")]
public sealed class ClientsController(IClientsService clientsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ClientSummaryDto>>> GetClients(CancellationToken cancellationToken)
    {
        var response = await clientsService.GetClientsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{clientId}")]
    public async Task<ActionResult<ClientDetailDto>> GetClient(string clientId, CancellationToken cancellationToken)
    {
        var response = await clientsService.GetClientAsync(clientId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ClientsWrite")]
    [HttpPost]
    public async Task<ActionResult<ClientDetailDto>> CreateClient(
        [FromBody] CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        var response = await clientsService.CreateClientAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Policy = "ClientsWrite")]
    [HttpPut("{clientId}")]
    public async Task<ActionResult<ClientDetailDto>> UpdateClient(
        string clientId,
        [FromBody] UpdateClientRequest request,
        CancellationToken cancellationToken)
    {
        var response = await clientsService.UpdateClientAsync(clientId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ClientsWrite")]
    [HttpPost("{clientId}/password-setup/resend")]
    public async Task<IActionResult> ResendPasswordSetup(string clientId, CancellationToken cancellationToken)
    {
        await clientsService.ResendPasswordSetupAsync(clientId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpDelete("{clientId}")]
    public async Task<IActionResult> DeleteClient(string clientId, CancellationToken cancellationToken)
    {
        await clientsService.DeleteClientAsync(clientId, cancellationToken);
        return NoContent();
    }
}
