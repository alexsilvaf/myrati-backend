using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.API.Realtime;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Route("api/v1/backoffice/notifications")]
public sealed class NotificationsController(INotificationsService notificationsService) : AuthenticatedControllerBase
{
    [Authorize(Policy = "BackofficeRead")]
    [HttpGet]
    public Task<NotificationFeedDto> Get([FromQuery] int limit = 12, CancellationToken cancellationToken = default) =>
        notificationsService.GetAsync(GetCurrentUserEmail(), NormalizeLimit(limit), cancellationToken);

    [Authorize(Policy = "BackofficeRead")]
    [HttpPost("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(string notificationId, CancellationToken cancellationToken = default)
    {
        await notificationsService.MarkAsReadAsync(GetCurrentUserEmail(), notificationId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "BackofficeRead")]
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        await notificationsService.MarkAllAsReadAsync(GetCurrentUserEmail(), cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "BackofficeRead")]
    [HttpGet("stream")]
    public async Task Stream([FromQuery] int limit = 12, CancellationToken cancellationToken = default)
    {
        var normalizedLimit = NormalizeLimit(limit);
        var email = GetCurrentUserEmail();
        NotificationFeedDto? currentSnapshot = null;

        SseWriter.Configure(Response);
        await SseWriter.WriteEventAsync(
            Response,
            "connected",
            new
            {
                channel = "notifications",
                connectedAt = DateTimeOffset.UtcNow,
                userEmail = email
            },
            cancellationToken);

        currentSnapshot = await notificationsService.GetAsync(email, normalizedLimit, cancellationToken);
        await SseWriter.WriteEventAsync(Response, "notifications.snapshot", currentSnapshot, cancellationToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            var nextSnapshot = await notificationsService.GetAsync(email, normalizedLimit, cancellationToken);
            await SseWriter.WriteEventAsync(
                Response,
                "heartbeat",
                new { channel = "notifications", at = DateTimeOffset.UtcNow },
                cancellationToken);

            if (CreateSignature(currentSnapshot) == CreateSignature(nextSnapshot))
            {
                continue;
            }

            currentSnapshot = nextSnapshot;
            await SseWriter.WriteEventAsync(Response, "notifications.snapshot", currentSnapshot, cancellationToken);
        }
    }

    private static int NormalizeLimit(int limit) => Math.Clamp(limit, 1, 50);

    private static string CreateSignature(NotificationFeedDto? snapshot)
    {
        if (snapshot is null)
        {
            return "empty";
        }

        return $"{snapshot.UnreadCount}:{string.Join('|', snapshot.Items.Select(item => $"{item.Id}:{item.Read}"))}";
    }
}
