using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface INotificationsService
{
    Task<NotificationFeedDto> GetAsync(string email, int limit = 12, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string email, string notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(string email, CancellationToken cancellationToken = default);
}
