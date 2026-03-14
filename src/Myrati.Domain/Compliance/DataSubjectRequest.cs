using Myrati.Domain.Common;

namespace Myrati.Domain.Compliance;

public sealed class DataSubjectRequest : Entity
{
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectEmail { get; set; } = string.Empty;
    public string SubjectDocument { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? ResolutionSummary { get; set; }
    public bool IdentityVerified { get; set; }
    public string? AssignedAdminUserId { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset DueAtUtc { get; set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
