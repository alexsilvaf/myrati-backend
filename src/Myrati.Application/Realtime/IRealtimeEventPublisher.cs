namespace Myrati.Application.Realtime;

public interface IRealtimeEventPublisher
{
    ValueTask PublishAsync(RealtimeEvent realtimeEvent, CancellationToken cancellationToken = default);
}
