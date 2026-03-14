namespace Myrati.Application.Contracts;

public sealed record ComplianceMetricsDto(
    int OpenDataSubjectRequests,
    int DueSoonDataSubjectRequests,
    int ActiveSecurityIncidents,
    int ActivitiesNeedingReview);

public sealed record DataSubjectRequestDto(
    string Id,
    string SubjectName,
    string SubjectEmail,
    string SubjectDocument,
    string RequestType,
    string Channel,
    string Status,
    string Details,
    string? ResolutionSummary,
    bool IdentityVerified,
    string? AssignedAdminUserId,
    string? AssignedAdminName,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? AcknowledgedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ProcessingActivityDto(
    string Id,
    string Name,
    string SystemName,
    string Purpose,
    string LegalBasis,
    string DataSubjectCategories,
    string PersonalDataCategories,
    string SharedWith,
    string RetentionPolicy,
    string SecurityMeasures,
    string OwnerArea,
    bool InternationalTransfer,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ReviewDueAtUtc);

public sealed record SecurityIncidentDto(
    string Id,
    string Title,
    string Description,
    string Severity,
    string Status,
    bool ContainsPersonalData,
    string AffectedDataSummary,
    string ImpactSummary,
    string MitigationSummary,
    bool NotifyAnpd,
    bool NotifyDataSubjects,
    string? AssignedAdminUserId,
    string? AssignedAdminName,
    DateTimeOffset DetectedAtUtc,
    DateTimeOffset? OccurredAtUtc,
    DateTimeOffset? ContainedAtUtc,
    DateTimeOffset? ReportedToAnpdAtUtc,
    DateTimeOffset? ReportedToDataSubjectsAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ComplianceSnapshotDto(
    ComplianceMetricsDto Metrics,
    IReadOnlyCollection<DataSubjectRequestDto> DataSubjectRequests,
    IReadOnlyCollection<ProcessingActivityDto> ProcessingActivities,
    IReadOnlyCollection<SecurityIncidentDto> SecurityIncidents);

public sealed record CreateDataSubjectRequestRequest(
    string SubjectName,
    string SubjectEmail,
    string SubjectDocument,
    string RequestType,
    string Channel,
    string Details,
    bool IdentityVerified,
    string? AssignedAdminUserId,
    DateTimeOffset? DueAtUtc);

public sealed record UpdateDataSubjectRequestRequest(
    string Status,
    string Details,
    string? ResolutionSummary,
    bool IdentityVerified,
    string? AssignedAdminUserId,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? AcknowledgedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record CreateProcessingActivityRequest(
    string Name,
    string SystemName,
    string Purpose,
    string LegalBasis,
    string DataSubjectCategories,
    string PersonalDataCategories,
    string SharedWith,
    string RetentionPolicy,
    string SecurityMeasures,
    string OwnerArea,
    bool InternationalTransfer,
    string Status,
    DateTimeOffset? ReviewDueAtUtc);

public sealed record UpdateProcessingActivityRequest(
    string Name,
    string SystemName,
    string Purpose,
    string LegalBasis,
    string DataSubjectCategories,
    string PersonalDataCategories,
    string SharedWith,
    string RetentionPolicy,
    string SecurityMeasures,
    string OwnerArea,
    bool InternationalTransfer,
    string Status,
    DateTimeOffset? ReviewDueAtUtc);

public sealed record CreateSecurityIncidentRequest(
    string Title,
    string Description,
    string Severity,
    string Status,
    bool ContainsPersonalData,
    string AffectedDataSummary,
    string ImpactSummary,
    string MitigationSummary,
    bool NotifyAnpd,
    bool NotifyDataSubjects,
    string? AssignedAdminUserId,
    DateTimeOffset DetectedAtUtc,
    DateTimeOffset? OccurredAtUtc,
    DateTimeOffset? ContainedAtUtc,
    DateTimeOffset? ReportedToAnpdAtUtc,
    DateTimeOffset? ReportedToDataSubjectsAtUtc);

public sealed record UpdateSecurityIncidentRequest(
    string Title,
    string Description,
    string Severity,
    string Status,
    bool ContainsPersonalData,
    string AffectedDataSummary,
    string ImpactSummary,
    string MitigationSummary,
    bool NotifyAnpd,
    bool NotifyDataSubjects,
    string? AssignedAdminUserId,
    DateTimeOffset DetectedAtUtc,
    DateTimeOffset? OccurredAtUtc,
    DateTimeOffset? ContainedAtUtc,
    DateTimeOffset? ReportedToAnpdAtUtc,
    DateTimeOffset? ReportedToDataSubjectsAtUtc);
