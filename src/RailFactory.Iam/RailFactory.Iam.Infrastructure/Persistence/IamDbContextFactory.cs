using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RailFactory.Iam.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tools at design time (e.g. migrations) when the app does not run (no connection string from Aspire).
/// Set ConnectionStrings__iamdb or use the default below for local PostgreSQL.
/// </summary>
internal sealed class IamDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public IamDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__iamdb")
            ?? "Host=localhost;Port=5432;Database=iam;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new IamDbContext(options);
    }
}
