using Myrati.Application.Realtime;

namespace Myrati.Application.Tests.Support;

public sealed class TestRealtimeEventPublisher : IRealtimeEventPublisher
{
    public List<RealtimeEvent> Events { get; } = [];

    public ValueTask PublishAsync(RealtimeEvent realtimeEvent, CancellationToken cancellationToken = default)
    {
        Events.Add(realtimeEvent);
        return ValueTask.CompletedTask;
    }
}
