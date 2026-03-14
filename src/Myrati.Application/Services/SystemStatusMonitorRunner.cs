using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Myrati.Application.Abstractions;
using Myrati.Domain.Public;

namespace Myrati.Application.Services;

public sealed class SystemStatusMonitorRunner(
    IServiceScopeFactory scopeFactory,
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<SystemStatusMonitorRunner> logger)
{
    private readonly ConcurrentDictionary<string, ComponentCheckStats> componentStats = new(StringComparer.Ordinal);

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var options = SystemStatusMonitorOptions.FromConfiguration(configuration);
        if (!options.Enabled || options.Components.Count == 0)
        {
            logger.LogInformation("Monitor publico de status desabilitado ou sem componentes configurados.");
            return;
        }

        var results = new List<SystemStatusCheckResult>(options.Components.Count);
        foreach (var component in options.Components.OrderBy(x => x.SortOrder))
        {
            results.Add(await CheckComponentAsync(component, options, cancellationToken));
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IMyratiDbContext>();
        var existingComponents = await dbContext.SystemComponentStatuses
            .ToListAsync(cancellationToken);

        foreach (var staleComponent in existingComponents.Where(x => results.All(result => result.Id != x.Id)))
        {
            dbContext.Remove(staleComponent);
        }

        foreach (var result in results)
        {
            var component = existingComponents.FirstOrDefault(x => x.Id == result.Id);
            if (component is null)
            {
                component = new SystemComponentStatus
                {
                    Id = result.Id
                };
                await dbContext.AddAsync(component, cancellationToken);
            }

            component.Name = result.Name;
            component.Status = result.Status;
            component.Uptime = result.UptimeDisplay;
            component.ResponseTime = result.ResponseTimeDisplay;
            component.SortOrder = result.SortOrder;
        }

        var metadata = await dbContext.SystemStatusMetadata.FirstOrDefaultAsync(cancellationToken);
        if (metadata is null)
        {
            metadata = new SystemStatusMetadata
            {
                Id = "SYS-MONITOR"
            };
            await dbContext.AddAsync(metadata, cancellationToken);
        }

        var saoPauloNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ResolveSaoPauloTimeZone());
        metadata.LastUpdatedDisplay = saoPauloNow.ToString("dd MMM yyyy 'às' HH:mm", new CultureInfo("pt-BR"));

        await UpsertDailyUptimeSampleAsync(dbContext, results, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SystemStatusCheckResult> CheckComponentAsync(
        MonitoredSystemComponent component,
        SystemStatusMonitorOptions options,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestCancellation.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));

            using var response = await httpClient.GetAsync(component.Url, requestCancellation.Token);
            stopwatch.Stop();

            var status = response.IsSuccessStatusCode
                ? stopwatch.ElapsedMilliseconds > options.DegradedResponseTimeMs
                    ? "degraded"
                    : "operational"
                : "outage";

            return CreateResult(component, status, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogWarning(ex, "Falha ao consultar health check do componente publico {ComponentName} em {ComponentUrl}.", component.Name, component.Url);
            return CreateResult(component, "outage", null);
        }
    }

    private SystemStatusCheckResult CreateResult(
        MonitoredSystemComponent component,
        string status,
        long? responseTimeMs)
    {
        var stats = componentStats.AddOrUpdate(
            component.Id,
            _ => new ComponentCheckStats(status is not "outage" ? 1 : 0, 1),
            (_, current) =>
            {
                current.TotalChecks++;
                if (status is not "outage")
                {
                    current.SuccessfulChecks++;
                }

                return current;
            });

        var uptime = stats.TotalChecks == 0
            ? "0.0%"
            : $"{(decimal)stats.SuccessfulChecks / stats.TotalChecks * 100m:0.0}%";

        return new SystemStatusCheckResult(
            component.Id,
            component.Name,
            status,
            uptime,
            responseTimeMs is null ? "sem resposta" : $"{responseTimeMs.Value} ms",
            component.SortOrder);
    }

    private static async Task UpsertDailyUptimeSampleAsync(
        IMyratiDbContext dbContext,
        IReadOnlyCollection<SystemStatusCheckResult> results,
        CancellationToken cancellationToken)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var todayId = $"UPT-{today:yyyyMMdd}";
        var todayLabel = today.ToString("dd MMM", new CultureInfo("pt-BR"));
        var aggregatePercentage = results.Count == 0
            ? 0m
            : Math.Round(results.Count(result => result.Status == "operational") * 100m / results.Count, 1);

        var samples = await dbContext.UptimeSamples
            .ToListAsync(cancellationToken);
        var sample = samples.FirstOrDefault(x => x.Id == todayId);
        if (sample is null)
        {
            sample = new UptimeSample
            {
                Id = todayId
            };
            await dbContext.AddAsync(sample, cancellationToken);
        }

        sample.Day = todayLabel;
        sample.Percentage = aggregatePercentage;

        var retainedSamples = samples
            .Where(x => x.Id != todayId)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .TakeLast(13)
            .ToList();
        retainedSamples.Add(sample);

        var sortedSamples = retainedSamples
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();

        for (var index = 0; index < sortedSamples.Count; index++)
        {
            sortedSamples[index].SortOrder = index + 1;
        }

        foreach (var staleSample in samples.Where(x => sortedSamples.All(retained => retained.Id != x.Id)))
        {
            dbContext.Remove(staleSample);
        }
    }

    private static TimeZoneInfo ResolveSaoPauloTimeZone()
    {
        foreach (var candidate in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}

public sealed class SystemStatusMonitorBackgroundService(
    SystemStatusMonitorRunner runner,
    IConfiguration configuration,
    ILogger<SystemStatusMonitorBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = SystemStatusMonitorOptions.FromConfiguration(configuration);
        if (!options.Enabled)
        {
            logger.LogInformation("Monitor publico de status desabilitado.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await runner.RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao atualizar o monitor publico de status.");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), stoppingToken);
        }
    }
}

internal sealed record SystemStatusMonitorOptions(
    bool Enabled,
    int IntervalSeconds,
    int RequestTimeoutSeconds,
    int DegradedResponseTimeMs,
    IReadOnlyList<MonitoredSystemComponent> Components)
{
    public static SystemStatusMonitorOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("SystemStatus:Monitor");
        var components = section.GetSection("Components")
            .GetChildren()
            .Select(child => new MonitoredSystemComponent(
                child["Id"]?.Trim() ?? string.Empty,
                child["Name"]?.Trim() ?? string.Empty,
                child["Url"]?.Trim() ?? string.Empty,
                int.TryParse(child["SortOrder"], out var sortOrder) ? sortOrder : 0))
            .Where(component =>
                !string.IsNullOrWhiteSpace(component.Id)
                && !string.IsNullOrWhiteSpace(component.Name)
                && !string.IsNullOrWhiteSpace(component.Url))
            .OrderBy(component => component.SortOrder)
            .ToArray();

        return new SystemStatusMonitorOptions(
            !string.Equals(section["Enabled"], "false", StringComparison.OrdinalIgnoreCase),
            int.TryParse(section["IntervalSeconds"], out var intervalSeconds) && intervalSeconds > 0 ? intervalSeconds : 15,
            int.TryParse(section["RequestTimeoutSeconds"], out var requestTimeoutSeconds) && requestTimeoutSeconds > 0 ? requestTimeoutSeconds : 5,
            int.TryParse(section["DegradedResponseTimeMs"], out var degradedResponseTimeMs) && degradedResponseTimeMs > 0 ? degradedResponseTimeMs : 1200,
            components);
    }
}

internal sealed record MonitoredSystemComponent(
    string Id,
    string Name,
    string Url,
    int SortOrder);

internal sealed class ComponentCheckStats(int successfulChecks, int totalChecks)
{
    public int SuccessfulChecks { get; set; } = successfulChecks;
    public int TotalChecks { get; set; } = totalChecks;
}

internal sealed record SystemStatusCheckResult(
    string Id,
    string Name,
    string Status,
    string UptimeDisplay,
    string ResponseTimeDisplay,
    int SortOrder);
