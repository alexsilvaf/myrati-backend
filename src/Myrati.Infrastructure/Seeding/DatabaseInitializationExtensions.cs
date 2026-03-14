using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Myrati.Infrastructure.Persistence;

namespace Myrati.Infrastructure.Seeding;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyratiDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<MyratiDbSeeder>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Myrati.Infrastructure.Seeding.DatabaseInitialization");

        await context.Database.EnsureCreatedAsync(cancellationToken);
        await DatabaseSchemaCompatibilityUpgrader.ApplyAsync(context, cancellationToken);
        await seeder.SeedAsync(context, cancellationToken);
        await ApplyAuditLogRetentionAsync(context, configuration, logger, cancellationToken);

        if (string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"],
            "Production",
            StringComparison.OrdinalIgnoreCase))
        {
            var bootstrapper = scope.ServiceProvider.GetRequiredService<ProductionSuperAdminPasswordSetupBootstrapper>();
            await bootstrapper.SendInvitationsAsync(context, cancellationToken);
        }
    }

    private static async Task ApplyAuditLogRetentionAsync(
        MyratiDbContext context,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var retentionDays = configuration.GetValue<int?>("Audit:RetentionDays") ?? 365;
        if (retentionDays <= 0)
        {
            logger.LogInformation("Retenção de audit logs desabilitada porque Audit:RetentionDays <= 0.");
            return;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var auditLogs = await context.AuditLogsSet.ToListAsync(cancellationToken);
        var staleLogs = auditLogs
            .Where(x => x.OccurredAtUtc < cutoff)
            .ToList();

        if (staleLogs.Count == 0)
        {
            return;
        }

        context.AuditLogsSet.RemoveRange(staleLogs);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Retenção de audit logs aplicada no startup. {Count} registros removidos antes de {CutoffUtc:o}.",
            staleLogs.Count,
            cutoff);
    }
}
