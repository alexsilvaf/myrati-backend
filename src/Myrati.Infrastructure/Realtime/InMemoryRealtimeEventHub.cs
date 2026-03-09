using System.Collections.Concurrent;
using System.Threading.Channels;
using Myrati.Application.Realtime;

namespace Myrati.Infrastructure.Realtime;

public sealed class InMemoryRealtimeEventHub : IRealtimeEventPublisher, IRealtimeEventStream
{
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions = new();

    public ValueTask PublishAsync(RealtimeEvent realtimeEvent, CancellationToken cancellationToken = default)
    {
        foreach (var subscription in _subscriptions.Values.Where(subscription => subscription.Channel == realtimeEvent.Channel))
        {
            subscription.Writer.TryWrite(realtimeEvent);
        }

        return ValueTask.CompletedTask;
    }

    public IRealtimeEventSubscription Subscribe(string channel, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var subscription = new Subscription(
            id,
            channel,
            Channel.CreateUnbounded<RealtimeEvent>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            }),
            RemoveSubscription);

        _subscriptions[id] = subscription;

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => RemoveSubscription(id));
        }

        return subscription;
    }

    private void RemoveSubscription(Guid id)
    {
        if (_subscriptions.TryRemove(id, out var subscription))
        {
            subscription.Writer.TryComplete();
        }
    }

    private sealed class Subscription(
        Guid id,
        string channel,
        Channel<RealtimeEvent> channelState,
        Action<Guid> onDispose) : IRealtimeEventSubscription
    {
        public string Channel { get; } = channel;

        public ChannelWriter<RealtimeEvent> Writer => channelState.Writer;

        public ChannelReader<RealtimeEvent> Reader => channelState.Reader;

        public ValueTask DisposeAsync()
        {
            onDispose(id);
            return ValueTask.CompletedTask;
        }
    }
}
