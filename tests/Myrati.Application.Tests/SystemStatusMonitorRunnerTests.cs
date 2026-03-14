using System.Net;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Myrati.Application.Abstractions;
using Myrati.Application.Services;
using Myrati.Infrastructure.Persistence;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class SystemStatusMonitorRunnerTests
{
    [Fact]
    public async Task RefreshAsync_CreatesPublicStatusComponentsFromHealthChecks()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<MyratiDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IMyratiDbContext>(provider => provider.GetRequiredService<MyratiDbContext>());

        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MyratiDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemStatus:Monitor:Enabled"] = "true",
                ["SystemStatus:Monitor:IntervalSeconds"] = "15",
                ["SystemStatus:Monitor:Components:0:Id"] = "STS-OK",
                ["SystemStatus:Monitor:Components:0:Name"] = "Serviço OK",
                ["SystemStatus:Monitor:Components:0:Url"] = "http://monitor/ok",
                ["SystemStatus:Monitor:Components:0:SortOrder"] = "1",
                ["SystemStatus:Monitor:Components:1:Id"] = "STS-FAIL",
                ["SystemStatus:Monitor:Components:1:Name"] = "Serviço Falho",
                ["SystemStatus:Monitor:Components:1:Url"] = "http://monitor/fail",
                ["SystemStatus:Monitor:Components:1:SortOrder"] = "2"
            })
            .Build();

        var runner = new SystemStatusMonitorRunner(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new HttpClient(new StubHttpMessageHandler(request =>
            {
                if (request.RequestUri?.AbsolutePath == "/ok")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            })),
            configuration,
            NullLogger<SystemStatusMonitorRunner>.Instance);

        await runner.RefreshAsync();

        await using var assertionScope = provider.CreateAsyncScope();
        var assertionDbContext = assertionScope.ServiceProvider.GetRequiredService<MyratiDbContext>();
        var components = await assertionDbContext.SystemComponentStatusesSet
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
        var metadata = await assertionDbContext.SystemStatusMetadataSet.FirstOrDefaultAsync();
        var samples = await assertionDbContext.UptimeSamplesSet.ToListAsync();

        Assert.Collection(
            components,
            component =>
            {
                Assert.Equal("STS-OK", component.Id);
                Assert.Equal("operational", component.Status);
            },
            component =>
            {
                Assert.Equal("STS-FAIL", component.Id);
                Assert.Equal("outage", component.Status);
            });
        Assert.NotNull(metadata);
        Assert.False(string.IsNullOrWhiteSpace(metadata!.LastUpdatedDisplay));
        Assert.Single(samples);
    }

    [Fact]
    public async Task RefreshAsync_CanRunConsecutivelyWithTheSameHttpClient()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<MyratiDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IMyratiDbContext>(provider => provider.GetRequiredService<MyratiDbContext>());

        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MyratiDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemStatus:Monitor:Enabled"] = "true",
                ["SystemStatus:Monitor:IntervalSeconds"] = "15",
                ["SystemStatus:Monitor:RequestTimeoutSeconds"] = "1",
                ["SystemStatus:Monitor:Components:0:Id"] = "STS-OK",
                ["SystemStatus:Monitor:Components:0:Name"] = "Serviço OK",
                ["SystemStatus:Monitor:Components:0:Url"] = "http://monitor/ok",
                ["SystemStatus:Monitor:Components:0:SortOrder"] = "1"
            })
            .Build();

        var requestCount = 0;
        var runner = new SystemStatusMonitorRunner(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new HttpClient(new StubHttpMessageHandler(_ =>
            {
                requestCount++;
                return new HttpResponseMessage(HttpStatusCode.OK);
            })),
            configuration,
            NullLogger<SystemStatusMonitorRunner>.Instance);

        await runner.RefreshAsync();
        await runner.RefreshAsync();

        Assert.Equal(2, requestCount);

        await using var assertionScope = provider.CreateAsyncScope();
        var assertionDbContext = assertionScope.ServiceProvider.GetRequiredService<MyratiDbContext>();
        var component = await assertionDbContext.SystemComponentStatusesSet.SingleAsync();

        Assert.Equal("STS-OK", component.Id);
        Assert.Equal("operational", component.Status);
        Assert.Matches(@"^100(?:[.,]0)?%$", component.Uptime);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
