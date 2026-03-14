using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Myrati.Application.Abstractions;
using Myrati.Domain.Auditing;
using Myrati.Infrastructure.Persistence;

namespace Myrati.Infrastructure.Auditing;

public sealed class AuditLogWriter(
    IServiceScopeFactory scopeFactory,
    ILogger<AuditLogWriter> logger) : IAuditLogWriter
{
    public async Task WriteAsync(AuditLogWriteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyratiDbContext>();

            await dbContext.AuditLogsSet.AddAsync(new AuditLog
            {
                Id = $"AUD-{Guid.NewGuid():N}".ToUpperInvariant(),
                OccurredAtUtc = request.OccurredAtUtc,
                ServiceName = string.IsNullOrWhiteSpace(request.ServiceName)
                    ? "myrati-backend"
                    : request.ServiceName,
                EventType = request.EventType,
                HttpMethod = request.HttpMethod,
                Path = request.Path,
                ResourceType = request.ResourceType,
                ResourceId = request.ResourceId,
                StatusCode = request.StatusCode,
                Outcome = request.Outcome,
                ActorUserId = request.ActorUserId,
                ActorEmail = request.ActorEmail,
                ActorRole = request.ActorRole,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                TraceIdentifier = request.TraceIdentifier
            }, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao persistir audit log para {Method} {Path}.", request.HttpMethod, request.Path);
        }
    }
}
