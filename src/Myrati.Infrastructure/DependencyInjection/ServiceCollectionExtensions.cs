using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Myrati.Application.Abstractions;
using Myrati.Application.Realtime;
using Myrati.Infrastructure.Persistence;
using Myrati.Infrastructure.Realtime;
using Myrati.Infrastructure.Seeding;
using Myrati.Infrastructure.Security;
using Npgsql;

namespace Myrati.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);

        services.AddDbContext<MyratiDbContext>(options =>
        {
            if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
                return;
            }

            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IMyratiDbContext>(provider => provider.GetRequiredService<MyratiDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<MyratiDbSeeder>();
        services.AddSingleton<InMemoryRealtimeEventHub>();
        services.AddSingleton<IRealtimeEventPublisher>(provider => provider.GetRequiredService<InMemoryRealtimeEventHub>());
        services.AddSingleton<IRealtimeEventStream>(provider => provider.GetRequiredService<InMemoryRealtimeEventHub>());

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var sqliteOverride = configuration["Testing:SqliteConnectionString"];
        if (!string.IsNullOrWhiteSpace(sqliteOverride))
        {
            return sqliteOverride;
        }

        var connectionString = configuration.GetConnectionString("MyratiDb")
            ?? throw new InvalidOperationException("ConnectionStrings:MyratiDb não configurada.");

        if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var environmentPassword = configuration["SPRING_DATASOURCE_PASSWORD"];
        if (string.IsNullOrWhiteSpace(environmentPassword))
        {
            return connectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Password = environmentPassword
        };

        return builder.ConnectionString;
    }
}
