using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Myrati.API.Realtime;
using Myrati.Application.Realtime;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class StreamController(
    IRealtimeEventStream realtimeEventStream,
    IDashboardService dashboardService,
    IPublicSiteService publicSiteService) : AuthenticatedControllerBase
{
    [Authorize(Policy = "BackofficeRead")]
    [HttpGet("backoffice/events")]
    public Task BackofficeEvents(CancellationToken cancellationToken) =>
        StreamAsync(
            Response,
            RealtimeChannels.Backoffice,
            "dashboard.snapshot",
            new
            {
                channel = RealtimeChannels.Backoffice,
                connectedAt = DateTimeOffset.UtcNow,
                userEmail = GetCurrentUserEmail()
            },
            snapshotFactory: dashboardService.GetAsync,
            snapshotInterval: TimeSpan.FromSeconds(15),
            cancellationToken);

    [AllowAnonymous]
    [EnableRateLimiting("public")]
    [HttpGet("public/status/stream")]
    public Task PublicStatus(CancellationToken cancellationToken) =>
        StreamAsync(
            Response,
            RealtimeChannels.PublicStatus,
            "status.snapshot",
            new
            {
                channel = RealtimeChannels.PublicStatus,
                connectedAt = DateTimeOffset.UtcNow
            },
            snapshotFactory: publicSiteService.GetSystemStatusAsync,
            snapshotInterval: TimeSpan.FromSeconds(30),
            cancellationToken);

    private async Task StreamAsync<TSnapshot>(
        HttpResponse response,
        string channel,
        string snapshotEvent,
        object connectedPayload,
        Func<CancellationToken, Task<TSnapshot>> snapshotFactory,
        TimeSpan snapshotInterval,
        CancellationToken cancellationToken)
    {
        SseWriter.Configure(response);

        await using var subscription = realtimeEventStream.Subscribe(channel, cancellationToken);

        await SseWriter.WriteEventAsync(response, "connected", connectedPayload, cancellationToken);
        await SseWriter.WriteEventAsync(response, snapshotEvent, await snapshotFactory(cancellationToken), cancellationToken);

        using var timer = new PeriodicTimer(snapshotInterval);

        while (!cancellationToken.IsCancellationRequested)
        {
            var readTask = subscription.Reader.WaitToReadAsync(cancellationToken).AsTask();
            var tickTask = timer.WaitForNextTickAsync(cancellationToken).AsTask();
            var completedTask = await Task.WhenAny(readTask, tickTask);

            if (completedTask == tickTask)
            {
                if (!await tickTask)
                {
                    break;
                }

                await SseWriter.WriteEventAsync(
                    response,
                    "heartbeat",
                    new { channel, at = DateTimeOffset.UtcNow },
                    cancellationToken);
                await SseWriter.WriteEventAsync(response, snapshotEvent, await snapshotFactory(cancellationToken), cancellationToken);
                continue;
            }

            if (!await readTask)
            {
                break;
            }

            while (subscription.Reader.TryRead(out var realtimeEvent))
            {
                await SseWriter.WriteEventAsync(
                    response,
                    realtimeEvent.EventType,
                    new
                    {
                        channel = realtimeEvent.Channel,
                        occurredAt = realtimeEvent.OccurredAt,
                        payload = realtimeEvent.Payload
                    },
                    cancellationToken);
            }
        }
    }
}
