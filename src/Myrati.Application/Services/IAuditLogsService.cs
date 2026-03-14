using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IAuditLogsService
{
    Task<AuditLogListResponse> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
}
