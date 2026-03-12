using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class StreamEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task BackofficeStream_ReturnsEventStreamAndDashboardSnapshot()
    {
        using var authClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await authClient.LoginAsAdminAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/backoffice/events?access_token={Uri.EscapeDataString(auth.AccessToken)}");
        using var response = await authClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var connectedMessage = await SseTestReader.ReadNextMessageAsync(reader, TimeSpan.FromSeconds(5));
        var snapshotMessage = await SseTestReader.ReadUntilEventAsync(reader, "dashboard.snapshot", TimeSpan.FromSeconds(5));

        Assert.Equal("connected", connectedMessage.Event);
        Assert.Contains("userEmail", connectedMessage.Data);
        Assert.Contains("TotalMonthlyRevenue", snapshotMessage.Data, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BackofficeStream_ReceivesClientCreatedEventAfterMutation()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var companyName = $"Cliente SSE Company {suffix}";

        using var streamClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var writerClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await writerClient.LoginAsAdminAsync();
        writerClient.UseBearerToken(auth.AccessToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/backoffice/events?access_token={Uri.EscapeDataString(auth.AccessToken)}");
        using var response = await streamClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        await SseTestReader.ReadNextMessageAsync(reader, TimeSpan.FromSeconds(5));
        await SseTestReader.ReadUntilEventAsync(reader, "dashboard.snapshot", TimeSpan.FromSeconds(5));

        var createResponse = await writerClient.PostAsJsonAsync(
            "/api/v1/backoffice/clients",
            new CreateClientRequest(
                $"Cliente SSE {suffix}",
                $"sse-{suffix}@myrati.com",
                "(11) 90000-0001",
                $"99.{suffix[..3]}.{suffix[3..6]}/0001-{suffix[6..8]}",
                "CNPJ",
                companyName,
                "Ativo"));
        createResponse.EnsureSuccessStatusCode();

        var eventMessage = await SseTestReader.ReadUntilEventAsync(reader, "client.created", TimeSpan.FromSeconds(5));

        Assert.Contains(companyName, eventMessage.Data);
        Assert.Contains("payload", eventMessage.Data);
    }

    [Fact]
    public async Task NotificationStream_ReturnsEventStreamAndSnapshot()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/backoffice/notifications/stream?limit=12&access_token={Uri.EscapeDataString(auth.AccessToken)}");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var connectedMessage = await SseTestReader.ReadNextMessageAsync(reader, TimeSpan.FromSeconds(5));
        var snapshotMessage = await SseTestReader.ReadUntilEventAsync(reader, "notifications.snapshot", TimeSpan.FromSeconds(5));

        Assert.Equal("connected", connectedMessage.Event);
        Assert.Contains("\"channel\":\"notifications\"", connectedMessage.Data, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UnreadCount", snapshotMessage.Data, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NotificationStream_ReceivesUpdatedSnapshotAfterMutation()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var companyName = $"Cliente Notificacao SSE {suffix}";

        using var streamClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var writerClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await writerClient.LoginAsAdminAsync();
        writerClient.UseBearerToken(auth.AccessToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/backoffice/notifications/stream?limit=12&access_token={Uri.EscapeDataString(auth.AccessToken)}");
        using var response = await streamClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        await SseTestReader.ReadNextMessageAsync(reader, TimeSpan.FromSeconds(5));
        await SseTestReader.ReadUntilEventAsync(reader, "notifications.snapshot", TimeSpan.FromSeconds(5));

        var createResponse = await writerClient.PostAsJsonAsync(
            "/api/v1/backoffice/clients",
            new CreateClientRequest(
                $"Cliente SSE {suffix}",
                $"notification-sse-{suffix}@myrati.com",
                "(11) 90000-0001",
                $"99.{suffix[..3]}.{suffix[3..6]}/0001-{suffix[6..8]}",
                "CNPJ",
                companyName,
                "Ativo"));
        createResponse.EnsureSuccessStatusCode();

        var snapshotMessage = await SseTestReader.ReadUntilEventAsync(
            reader,
            "notifications.snapshot",
            TimeSpan.FromSeconds(10));

        Assert.Contains("Cliente criado", snapshotMessage.Data);
        Assert.Contains(companyName, snapshotMessage.Data);
    }

    [Fact]
    public async Task PublicStatusStream_ReturnsEventStreamAndStatusSnapshot()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/public/status/stream");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        await SseTestReader.ReadNextMessageAsync(reader, TimeSpan.FromSeconds(5));
        var snapshotMessage = await SseTestReader.ReadUntilEventAsync(reader, "status.snapshot", TimeSpan.FromSeconds(5));

        Assert.Contains("OverallStatus", snapshotMessage.Data, StringComparison.OrdinalIgnoreCase);
    }
}
