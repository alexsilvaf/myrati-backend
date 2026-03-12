namespace Myrati.Application.Services;

public interface IBackofficeNotificationPublisher
{
    Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default);
}
