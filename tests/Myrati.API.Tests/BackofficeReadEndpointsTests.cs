using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class BackofficeReadEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task BackofficeReadEndpoints_ReturnExpectedSnapshots()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var meResponse = await client.GetAsync("/api/v1/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<AuthUserDto>();

        var dashboardResponse = await client.GetAsync("/api/v1/backoffice/dashboard");
        dashboardResponse.EnsureSuccessStatusCode();
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardResponse>();

        var transactionsResponse = await client.GetAsync("/api/v1/backoffice/transactions");
        transactionsResponse.EnsureSuccessStatusCode();
        var transactions = await transactionsResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<CashTransactionDto>>();

        var productsResponse = await client.GetAsync("/api/v1/backoffice/products");
        productsResponse.EnsureSuccessStatusCode();
        var products = await productsResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ProductSummaryDto>>();

        var productDetailResponse = await client.GetAsync("/api/v1/backoffice/products/PRD-001");
        productDetailResponse.EnsureSuccessStatusCode();
        var productDetail = await productDetailResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        var kanbanResponse = await client.GetAsync("/api/v1/backoffice/products/PRD-004/kanban");
        kanbanResponse.EnsureSuccessStatusCode();
        var kanban = await kanbanResponse.Content.ReadFromJsonAsync<ProductKanbanDto>();

        var clientsResponse = await client.GetAsync("/api/v1/backoffice/clients");
        clientsResponse.EnsureSuccessStatusCode();
        var clients = await clientsResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ClientSummaryDto>>();

        var clientDetailResponse = await client.GetAsync("/api/v1/backoffice/clients/CLI-001");
        clientDetailResponse.EnsureSuccessStatusCode();
        var clientDetail = await clientDetailResponse.Content.ReadFromJsonAsync<ClientDetailDto>();

        var usersResponse = await client.GetAsync("/api/v1/backoffice/users?status=Online");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<UserDirectoryItemDto>>();

        var settingsResponse = await client.GetAsync("/api/v1/backoffice/settings");
        settingsResponse.EnsureSuccessStatusCode();
        var settings = await settingsResponse.Content.ReadFromJsonAsync<SettingsSnapshotDto>();

        var profileResponse = await client.GetAsync("/api/v1/backoffice/profile");
        profileResponse.EnsureSuccessStatusCode();
        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileSnapshotDto>();

        var notificationSuffix = Guid.NewGuid().ToString("N")[..8];
        var notificationSeedResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/clients",
            new CreateClientRequest(
                $"Cliente Snapshot {notificationSuffix}",
                $"snapshot-{notificationSuffix}@myrati.com",
                "(11) 90000-0001",
                $"99.{notificationSuffix[..3]}.{notificationSuffix[3..6]}/0001-{notificationSuffix[6..8]}",
                "CNPJ",
                $"Cliente Snapshot Company {notificationSuffix}",
                "Ativo"));
        notificationSeedResponse.EnsureSuccessStatusCode();

        var notificationsResponse = await client.GetAsync("/api/v1/backoffice/notifications?limit=12");
        notificationsResponse.EnsureSuccessStatusCode();
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<NotificationFeedDto>();

        Assert.NotNull(me);
        Assert.Equal("admin@myrati.com", me.Email);

        Assert.NotNull(dashboard);
        Assert.NotEmpty(dashboard.MonthlyRevenue);
        Assert.NotEmpty(dashboard.RevenueByProduct);
        Assert.NotEmpty(dashboard.Alerts);
        Assert.NotEmpty(dashboard.ProductHealth);
        Assert.NotEmpty(dashboard.TopClients);
        Assert.True(dashboard.AvailableBalance > 0);

        Assert.NotNull(transactions);
        Assert.NotEmpty(transactions);

        Assert.NotNull(products);
        Assert.NotEmpty(products);

        Assert.NotNull(productDetail);
        Assert.Equal("PRD-001", productDetail.Id);
        Assert.NotEmpty(productDetail.Plans);
        Assert.NotEmpty(productDetail.Collaborators);
        Assert.Equal("subscription", productDetail.SalesStrategy);

        Assert.NotNull(kanban);
        Assert.NotEmpty(kanban.Sprints);
        Assert.NotEmpty(kanban.Tasks);

        Assert.NotNull(clients);
        Assert.NotEmpty(clients);

        Assert.NotNull(clientDetail);
        Assert.Equal("CLI-001", clientDetail.Id);
        Assert.NotEmpty(clientDetail.Licenses);

        Assert.NotNull(users);
        Assert.NotEmpty(users);

        Assert.NotNull(settings);
        Assert.NotEmpty(settings.ApiKeys);
        Assert.NotEmpty(settings.TeamMembers);

        Assert.NotNull(profile);
        Assert.Equal("admin@myrati.com", profile.Profile.Email);
        Assert.NotEmpty(profile.ActiveSessions);
        Assert.NotEmpty(profile.ActivityLog);

        Assert.NotNull(notifications);
        Assert.NotEmpty(notifications.Items);
        Assert.True(notifications.UnreadCount >= 1);
    }
}
