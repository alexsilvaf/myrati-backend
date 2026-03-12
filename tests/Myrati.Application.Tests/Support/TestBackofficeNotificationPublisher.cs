using Myrati.Application.Services;

namespace Myrati.Application.Tests.Support;

public sealed class TestBackofficeNotificationPublisher : IBackofficeNotificationPublisher
{
    public List<(string EventType, object Payload)> Events { get; } = [];

    public Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        Events.Add((eventType, payload));
        return Task.CompletedTask;
    }
}
