using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Myrati.API.Tests.Support;
using Myrati.Infrastructure.Persistence;
using Xunit;

namespace Myrati.API.Tests;

public sealed class AuditLogEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetProfile_CreatesStructuredAuditLog()
    {
        var client = factory.CreateClient();
        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var response = await client.GetAsync("/api/v1/backoffice/profile");
        response.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyratiDbContext>();

        var auditLog = (await dbContext.AuditLogsSet
                .Where(x => x.Path == "/api/v1/backoffice/profile")
                .ToListAsync())
            .OrderByDescending(x => x.OccurredAtUtc)
            .FirstOrDefault();

        Assert.NotNull(auditLog);
        Assert.Equal("success", auditLog.Outcome);
        Assert.Equal(200, auditLog.StatusCode);
        Assert.Equal("GET", auditLog.HttpMethod);
        Assert.Equal("admin@myrati.com", auditLog.ActorEmail);
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsRecentAuditEntries()
    {
        var client = factory.CreateClient();
        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var profileResponse = await client.GetAsync("/api/v1/backoffice/profile");
        profileResponse.EnsureSuccessStatusCode();

        var auditResponse = await client.GetAsync("/api/v1/backoffice/audit-logs?limit=10");
        auditResponse.EnsureSuccessStatusCode();

        var payload = await auditResponse.Content.ReadAsStringAsync();

        Assert.Contains("\"retentionDays\":365", payload);
        Assert.Contains("\"path\":\"/api/v1/backoffice/profile\"", payload);
    }
}
