using Microsoft.Extensions.Configuration;
using Myrati.Application.Abstractions;

namespace Myrati.Infrastructure.Auditing;

public sealed class AuditRetentionSettings(IConfiguration configuration) : IAuditRetentionSettings
{
    public int RetentionDays { get; } = configuration.GetValue<int?>("Audit:RetentionDays") ?? 365;
}
