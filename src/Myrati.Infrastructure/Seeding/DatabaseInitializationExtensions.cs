using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        await context.Database.EnsureCreatedAsync(cancellationToken);
        await DatabaseSchemaCompatibilityUpgrader.ApplyAsync(context, cancellationToken);
        await seeder.SeedAsync(context, cancellationToken);

        if (string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"],
            "Production",
            StringComparison.OrdinalIgnoreCase))
        {
            var bootstrapper = scope.ServiceProvider.GetRequiredService<ProductionSuperAdminPasswordSetupBootstrapper>();
            await bootstrapper.SendInvitationsAsync(context, cancellationToken);
        }
    }
}
