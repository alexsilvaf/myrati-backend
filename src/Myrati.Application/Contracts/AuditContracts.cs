namespace Myrati.Application.Contracts;

public sealed record AuditLogDto(
    string Id,
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

public sealed record AuditLogListResponse(
    int RetentionDays,
    IReadOnlyCollection<AuditLogDto> Items);
