using Myrati.Domain.Common;

namespace Myrati.Domain.Compliance;

public sealed class SecurityIncidentRecord : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool ContainsPersonalData { get; set; }
    public string AffectedDataSummary { get; set; } = string.Empty;
    public string ImpactSummary { get; set; } = string.Empty;
    public string MitigationSummary { get; set; } = string.Empty;
    public bool NotifyAnpd { get; set; }
    public bool NotifyDataSubjects { get; set; }
    public string? AssignedAdminUserId { get; set; }
    public DateTimeOffset DetectedAtUtc { get; set; }
    public DateTimeOffset? OccurredAtUtc { get; set; }
    public DateTimeOffset? ContainedAtUtc { get; set; }
    public DateTimeOffset? ReportedToAnpdAtUtc { get; set; }
    public DateTimeOffset? ReportedToDataSubjectsAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
