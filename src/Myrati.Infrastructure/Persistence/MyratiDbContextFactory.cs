using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Myrati.Infrastructure.Persistence;

public sealed class MyratiDbContextFactory : IDesignTimeDbContextFactory<MyratiDbContext>
{
    public MyratiDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MYRATI_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=myrati;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<MyratiDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new MyratiDbContext(optionsBuilder.Options);
    }
}
