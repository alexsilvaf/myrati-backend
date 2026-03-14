namespace Myrati.Application.Abstractions;

public interface IAuditRetentionSettings
{
    int RetentionDays { get; }
}
