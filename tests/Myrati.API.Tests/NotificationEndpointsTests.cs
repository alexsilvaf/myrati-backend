using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class NotificationEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task NotificationFeed_CanMarkSingleNotificationAsRead()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/clients",
            new CreateClientRequest(
                $"Cliente Notificacao Item {suffix}",
                $"item-{suffix}@myrati.com",
                "(11) 90000-0001",
                $"99.{suffix[..3]}.{suffix[3..6]}/0001-{suffix[6..8]}",
                "CNPJ",
                $"Cliente Notificacao Item Company {suffix}",
                "Ativo"));
        createResponse.EnsureSuccessStatusCode();

        var beforeResponse = await client.GetAsync("/api/v1/backoffice/notifications?limit=12");
        beforeResponse.EnsureSuccessStatusCode();
        var before = await beforeResponse.Content.ReadFromJsonAsync<NotificationFeedDto>();

        Assert.NotNull(before);
        var target = Assert.Single(before.Items.Where(item => !item.Read).Take(1));

        var markResponse = await client.PostAsync($"/api/v1/backoffice/notifications/{target.Id}/read", content: null);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, markResponse.StatusCode);

        var afterResponse = await client.GetAsync("/api/v1/backoffice/notifications?limit=12");
        afterResponse.EnsureSuccessStatusCode();
        var after = await afterResponse.Content.ReadFromJsonAsync<NotificationFeedDto>();

        Assert.NotNull(after);
        var updated = Assert.Single(after.Items, item => item.Id == target.Id);
        Assert.True(updated.Read);
        Assert.True(after.UnreadCount <= before.UnreadCount - 1);
    }

    [Fact]
    public async Task NotificationFeed_CanMarkAllNotificationsAsRead()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/clients",
            new CreateClientRequest(
                $"Cliente Notificacao {suffix}",
                $"notificacao-{suffix}@myrati.com",
                "(11) 90000-0001",
                $"99.{suffix[..3]}.{suffix[3..6]}/0001-{suffix[6..8]}",
                "CNPJ",
                $"Cliente Notificacao Company {suffix}",
                "Ativo"));
        createResponse.EnsureSuccessStatusCode();

        var beforeResponse = await client.GetAsync("/api/v1/backoffice/notifications?limit=12");
        beforeResponse.EnsureSuccessStatusCode();
        var before = await beforeResponse.Content.ReadFromJsonAsync<NotificationFeedDto>();

        Assert.NotNull(before);
        Assert.True(before.UnreadCount >= 1);

        var markAllResponse = await client.PostAsync("/api/v1/backoffice/notifications/read-all", content: null);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, markAllResponse.StatusCode);

        var afterResponse = await client.GetAsync("/api/v1/backoffice/notifications?limit=12");
        afterResponse.EnsureSuccessStatusCode();
        var after = await afterResponse.Content.ReadFromJsonAsync<NotificationFeedDto>();

        Assert.NotNull(after);
        Assert.Equal(0, after.UnreadCount);
        Assert.All(after.Items, item => Assert.True(item.Read));
    }
}
