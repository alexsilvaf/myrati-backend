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

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost]
    public async Task<ActionResult<ClientDetailDto>> CreateClient(
        [FromBody] CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        var response = await clientsService.CreateClientAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetClient), new { clientId = response.Id }, response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPut("{clientId}")]
    public async Task<ActionResult<ClientDetailDto>> UpdateClient(
        string clientId,
        [FromBody] UpdateClientRequest request,
        CancellationToken cancellationToken)
    {
        var response = await clientsService.UpdateClientAsync(clientId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpDelete("{clientId}")]
    public async Task<IActionResult> DeleteClient(string clientId, CancellationToken cancellationToken)
    {
        await clientsService.DeleteClientAsync(clientId, cancellationToken);
        return NoContent();
    }
}
