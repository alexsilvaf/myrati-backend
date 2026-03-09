using Microsoft.EntityFrameworkCore;
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

        await context.Database.EnsureCreatedAsync(cancellationToken);
        await seeder.SeedAsync(context, cancellationToken);
    }
}
