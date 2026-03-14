using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "BackofficeWrite")]
[Route("api/v1/backoffice/audit-logs")]
public sealed class AuditLogsController(IAuditLogsService auditLogsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AuditLogListResponse>> Get(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var response = await auditLogsService.GetRecentAsync(limit, cancellationToken);
        return Ok(response);
    }
}
