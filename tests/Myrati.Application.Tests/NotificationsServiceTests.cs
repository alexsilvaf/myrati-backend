using Myrati.Application.Common.Exceptions;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Domain.Notifications;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class NotificationsServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsNotificationsForUser()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        await SeedNotificationsAsync(scope);
        var service = new NotificationsService(scope.Context);

        var result = await service.GetAsync("admin@myrati.com", 10);

        Assert.Equal(1, result.UnreadCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAsync_ClampsLimitTo50()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        await SeedNotificationsAsync(scope);
        var service = new NotificationsService(scope.Context);

        var result = await service.GetAsync("admin@myrati.com", 999);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithInvalidId_ThrowsEntityNotFoundException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var service = new NotificationsService(scope.Context);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.MarkAsReadAsync("admin@myrati.com", "NOTIF-INVALID"));
    }

    [Fact]
    public async Task MarkAllAsReadAsync_SetsReadAtOnAllUnread()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        await SeedNotificationsAsync(scope);
        var service = new NotificationsService(scope.Context);

        await service.MarkAllAsReadAsync("admin@myrati.com");

        var result = await service.GetAsync("admin@myrati.com", 10);
        Assert.Equal(0, result.UnreadCount);
        Assert.All(result.Items, item => Assert.True(item.Read));
    }

    private static async Task SeedNotificationsAsync(SeededDbContextScope scope)
    {
        await scope.Context.AddAsync(new AdminNotification
        {
            Id = "NOTIF-001",
            RecipientAdminUserId = "TM-001",
            EventType = "client.created",
            Title = "Novo cliente cadastrado",
            Description = "Cliente TechCorp foi adicionado",
            Type = "info",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            ReadAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        });
        await scope.Context.AddAsync(new AdminNotification
        {
            Id = "NOTIF-002",
            RecipientAdminUserId = "TM-001",
            EventType = "license.expiring",
            Title = "Licenca expirando",
            Description = "Licenca LIC-001 expira em 30 dias",
            Type = "warning",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            ReadAt = null
        });
        await scope.Context.SaveChangesAsync();
    }
}
