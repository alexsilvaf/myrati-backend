using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public sealed class AuditLogsService(
    IMyratiDbContext dbContext,
    IAuditRetentionSettings auditRetentionSettings) : IAuditLogsService
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;

    public async Task<AuditLogListResponse> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Clamp(limit <= 0 ? DefaultLimit : limit, 1, MaxLimit);

        var items = (await dbContext.AuditLogs
            .Select(x => new AuditLogDto(
                x.Id,
                x.OccurredAtUtc,
                x.ServiceName,
                x.EventType,
                x.HttpMethod,
                x.Path,
                x.ResourceType,
                x.ResourceId,
                x.StatusCode,
                x.Outcome,
                x.ActorUserId,
                x.ActorEmail,
                x.ActorRole,
                x.IpAddress,
                x.UserAgent,
                x.TraceIdentifier))
            .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(effectiveLimit)
            .ToArray();

        return new AuditLogListResponse(auditRetentionSettings.RetentionDays, items);
    }
}
