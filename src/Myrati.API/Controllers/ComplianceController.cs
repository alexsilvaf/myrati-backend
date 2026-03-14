using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "BackofficeRead")]
[Route("api/v1/backoffice/compliance")]
public sealed class ComplianceController(IComplianceService complianceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ComplianceSnapshotDto>> Get(CancellationToken cancellationToken)
    {
        var response = await complianceService.GetSnapshotAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost("data-subject-requests")]
    public async Task<ActionResult<DataSubjectRequestDto>> CreateDataSubjectRequest(
        [FromBody] CreateDataSubjectRequestRequest request,
        CancellationToken cancellationToken)
    {
        var response = await complianceService.CreateDataSubjectRequestAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPut("data-subject-requests/{requestId}")]
    public async Task<ActionResult<DataSubjectRequestDto>> UpdateDataSubjectRequest(
        string requestId,
        [FromBody] UpdateDataSubjectRequestRequest request,
        CancellationToken cancellationToken)
    {
        var response = await complianceService.UpdateDataSubjectRequestAsync(requestId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost("processing-activities")]
    public async Task<ActionResult<ProcessingActivityDto>> CreateProcessingActivity(
        [FromBody] CreateProcessingActivityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await complianceService.CreateProcessingActivityAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPut("processing-activities/{activityId}")]
    public async Task<ActionResult<ProcessingActivityDto>> UpdateProcessingActivity(
        string activityId,
        [FromBody] UpdateProcessingActivityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await complianceService.UpdateProcessingActivityAsync(activityId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost("security-incidents")]
    public async Task<ActionResult<SecurityIncidentDto>> CreateSecurityIncident(
        [FromBody] CreateSecurityIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await complianceService.CreateSecurityIncidentAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPut("security-incidents/{incidentId}")]
    public async Task<ActionResult<SecurityIncidentDto>> UpdateSecurityIncident(
        string incidentId,
        [FromBody] UpdateSecurityIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await complianceService.UpdateSecurityIncidentAsync(incidentId, request, cancellationToken);
        return Ok(response);
    }
}
