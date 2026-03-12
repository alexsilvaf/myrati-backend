using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Myrati.API.Tests.Support;
using Myrati.Application.Abstractions;
using Xunit;

namespace Myrati.API.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string SharedDatabasePath = Path.Combine(
        Path.GetTempPath(),
        $"myrati-api-tests-{Environment.ProcessId}.db");
    private string? _databasePath;

    public TestPasswordSetupEmailSender PasswordSetupEmailSender { get; } = new();

    static CustomWebApplicationFactory()
    {
        if (File.Exists(SharedDatabasePath))
        {
            File.Delete(SharedDatabasePath);
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__MyratiDb", $"Data Source={SharedDatabasePath}");
        Environment.SetEnvironmentVariable("Jwt__Key", "TEST_SECRET_KEY_12345678901234567890");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _databasePath = SharedDatabasePath;

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MyratiDb"] = $"Data Source={_databasePath}",
                ["Jwt:Key"] = "TEST_SECRET_KEY_12345678901234567890",
                ["Seeding:IncludeDemoData"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPasswordSetupEmailSender>();
            services.AddSingleton(PasswordSetupEmailSender);
            services.AddSingleton<IPasswordSetupEmailSender>(PasswordSetupEmailSender);
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
