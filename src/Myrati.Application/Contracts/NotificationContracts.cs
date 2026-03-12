namespace Myrati.Application.Contracts;

public sealed record NotificationItemDto(
    string Id,
    string Title,
    string Description,
    string Time,
    bool Read,
    string Type);

public sealed record NotificationFeedDto(
    int UnreadCount,
    IReadOnlyCollection<NotificationItemDto> Items);
