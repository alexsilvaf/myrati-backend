namespace Myrati.Application.Abstractions;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditLogWriteRequest request, CancellationToken cancellationToken = default);
}

public sealed record AuditLogWriteRequest(
    DateTimeOffset OccurredAtUtc,
    string ServiceName,
    string EventType,
    string HttpMethod,
    string Path,
    string? ResourceType,
    string? ResourceId,
    int StatusCode,
    string Outcome,
    string? ActorUserId,
    string? ActorEmail,
    string? ActorRole,
    string? IpAddress,
    string? UserAgent,
    string TraceIdentifier);
