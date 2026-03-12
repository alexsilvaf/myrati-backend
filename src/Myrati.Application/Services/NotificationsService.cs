using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public sealed class NotificationsService(IMyratiDbContext dbContext) : INotificationsService
{
    public async Task<NotificationFeedDto> GetAsync(
        string email,
        int limit = 12,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserByEmailAsync(email, cancellationToken);
        var normalizedLimit = Math.Clamp(limit, 1, 50);

        var items = await dbContext.AdminNotifications
            .Where(notification => notification.RecipientAdminUserId == user.Id)
            .ToListAsync(cancellationToken);

        var unreadCount = await dbContext.AdminNotifications
            .CountAsync(
                notification => notification.RecipientAdminUserId == user.Id && notification.ReadAt == null,
                cancellationToken);

        return new NotificationFeedDto(
            unreadCount,
            items.OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Take(normalizedLimit)
                .Select(
                    item => new NotificationItemDto(
                        item.Id,
                        item.Title,
                        item.Description,
                        FormatRelativeTime(item.CreatedAt),
                        item.ReadAt != null,
                        item.Type))
                .ToArray());
    }

    public async Task MarkAsReadAsync(
        string email,
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserByEmailAsync(email, cancellationToken);
        var notification = await dbContext.AdminNotifications
            .FirstOrDefaultAsync(
                item => item.Id == notificationId && item.RecipientAdminUserId == user.Id,
                cancellationToken)
            ?? throw new EntityNotFoundException("Notificacao", notificationId);

        if (notification.ReadAt != null)
        {
            return;
        }

        notification.ReadAt = DateTimeOffset.UtcNow;
        dbContext.Update(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByEmailAsync(email, cancellationToken);
        var unreadNotifications = await dbContext.AdminNotifications
            .Where(item => item.RecipientAdminUserId == user.Id && item.ReadAt == null)
            .ToListAsync(cancellationToken);

        if (unreadNotifications.Count == 0)
        {
            return;
        }

        var readAt = DateTimeOffset.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.ReadAt = readAt;
            dbContext.Update(notification);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Domain.Identity.AdminUser> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await dbContext.AdminUsers
            .FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken)
            ?? throw new EntityNotFoundException("Usuario", email);
    }

    private static string FormatRelativeTime(DateTimeOffset createdAt)
    {
        var elapsed = DateTimeOffset.UtcNow - createdAt;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "Agora";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return $"Ha {Math.Max(1, (int)elapsed.TotalMinutes)} min";
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            return $"Ha {Math.Max(1, (int)elapsed.TotalHours)} h";
        }

        return $"Ha {Math.Max(1, (int)elapsed.TotalDays)} d";
    }
}
