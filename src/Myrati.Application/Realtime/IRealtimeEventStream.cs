using System.Threading.Channels;

namespace Myrati.Application.Realtime;

public interface IRealtimeEventStream
{
    IRealtimeEventSubscription Subscribe(string channel, CancellationToken cancellationToken = default);
}

public interface IRealtimeEventSubscription : IAsyncDisposable
{
    ChannelReader<RealtimeEvent> Reader { get; }
}
