using Myrati.Application.Abstractions;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Domain.Auditing;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class AuditLogsServiceTests
{
    [Fact]
    public async Task GetRecentAsync_ReturnsAuditLogsOrderedByDate()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var older = new AuditLog
        {
            Id = "AL-001",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            ServiceName = "API",
            EventType = "client.created",
            HttpMethod = "POST",
            Path = "/api/clients",
            StatusCode = 201,
            Outcome = "success",
            TraceIdentifier = "trace-1"
        };
        var newer = new AuditLog
        {
            Id = "AL-002",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            ServiceName = "API",
            EventType = "client.updated",
            HttpMethod = "PUT",
            Path = "/api/clients/CLI-001",
            StatusCode = 200,
            Outcome = "success",
            TraceIdentifier = "trace-2"
        };
        await scope.Context.AddAsync(older);
        await scope.Context.AddAsync(newer);
        await scope.Context.SaveChangesAsync();

        var service = new AuditLogsService(scope.Context, new TestAuditRetentionSettings());

        var result = await service.GetRecentAsync(10);

        Assert.Equal(90, result.RetentionDays);
        Assert.Equal(2, result.Items.Count);
        var items = result.Items.ToArray();
        Assert.Equal("AL-002", items[0].Id);
        Assert.Equal("AL-001", items[1].Id);
    }

    [Fact]
    public async Task GetRecentAsync_ClampsLimitToMaximum()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        await scope.Context.AddAsync(new AuditLog
        {
            Id = "AL-001",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ServiceName = "API",
            EventType = "test",
            HttpMethod = "GET",
            Path = "/api/test",
            StatusCode = 200,
            Outcome = "success",
            TraceIdentifier = "trace-1"
        });
        await scope.Context.SaveChangesAsync();

        var service = new AuditLogsService(scope.Context, new TestAuditRetentionSettings());

        var result = await service.GetRecentAsync(9999);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetRecentAsync_WithZeroLimit_UsesDefaultLimit()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        await scope.Context.AddAsync(new AuditLog
        {
            Id = "AL-001",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ServiceName = "API",
            EventType = "test",
            HttpMethod = "GET",
            Path = "/api/test",
            StatusCode = 200,
            Outcome = "success",
            TraceIdentifier = "trace-1"
        });
        await scope.Context.SaveChangesAsync();

        var service = new AuditLogsService(scope.Context, new TestAuditRetentionSettings());

        var result = await service.GetRecentAsync(0);

        Assert.Single(result.Items);
    }
}

file sealed class TestAuditRetentionSettings : IAuditRetentionSettings
{
    public int RetentionDays => 90;
}
