using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Myrati.Infrastructure.Persistence;
using Myrati.Infrastructure.Seeding;
using Myrati.Infrastructure.Security;

namespace Myrati.Application.Tests.Support;

public sealed class SeededDbContextScope : IAsyncDisposable
{
    private SeededDbContextScope(SqliteConnection connection, MyratiDbContext context)
    {
        Connection = connection;
        Context = context;
    }

    public SqliteConnection Connection { get; }

    public MyratiDbContext Context { get; }

    public static async Task<SeededDbContextScope> CreateAsync(bool seed = true)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MyratiDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new MyratiDbContext(options);
        await context.Database.EnsureCreatedAsync();

        if (seed)
        {
            var seeder = new MyratiDbSeeder(new PasswordHasher());
            await seeder.SeedAsync(context);
        }

        return new SeededDbContextScope(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
