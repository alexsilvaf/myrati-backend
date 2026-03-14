using Myrati.Domain.Common;

namespace Myrati.Domain.Auditing;

public sealed class AuditLog : Entity
{
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public int StatusCode { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string? ActorRole { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string TraceIdentifier { get; set; } = string.Empty;
}
