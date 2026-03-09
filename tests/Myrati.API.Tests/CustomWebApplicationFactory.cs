using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Myrati.API.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string? _databasePath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"myrati-api-tests-{Guid.NewGuid():N}.db");

        Environment.SetEnvironmentVariable("ConnectionStrings__MyratiDb", $"Data Source={_databasePath}");
        Environment.SetEnvironmentVariable("Jwt__Key", "TEST_SECRET_KEY_12345678901234567890");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MyratiDb"] = $"Data Source={_databasePath}",
                ["Jwt:Key"] = "TEST_SECRET_KEY_12345678901234567890"
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__MyratiDb", null);
        Environment.SetEnvironmentVariable("Jwt__Key", null);

        await base.DisposeAsync();

        if (_databasePath is not null && File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
            }
        }
    }
}
