namespace Myrati.Application.Realtime;

public sealed record RealtimeEvent(
    string Channel,
    string EventType,
    DateTimeOffset OccurredAt,
    object? Payload = null);
