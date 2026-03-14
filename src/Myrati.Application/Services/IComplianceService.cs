using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IComplianceService
{
    Task<ComplianceSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);
    Task<DataSubjectRequestDto> CreateDataSubjectRequestAsync(
        CreateDataSubjectRequestRequest request,
        CancellationToken cancellationToken = default);
    Task<DataSubjectRequestDto> UpdateDataSubjectRequestAsync(
        string requestId,
        UpdateDataSubjectRequestRequest request,
        CancellationToken cancellationToken = default);
    Task<ProcessingActivityDto> CreateProcessingActivityAsync(
        CreateProcessingActivityRequest request,
        CancellationToken cancellationToken = default);
    Task<ProcessingActivityDto> UpdateProcessingActivityAsync(
        string activityId,
        UpdateProcessingActivityRequest request,
        CancellationToken cancellationToken = default);
    Task<SecurityIncidentDto> CreateSecurityIncidentAsync(
        CreateSecurityIncidentRequest request,
        CancellationToken cancellationToken = default);
    Task<SecurityIncidentDto> UpdateSecurityIncidentAsync(
        string incidentId,
        UpdateSecurityIncidentRequest request,
        CancellationToken cancellationToken = default);
}
