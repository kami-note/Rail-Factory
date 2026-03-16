using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RailFactory.IAM.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tools at design time (e.g. migrations) to create IamDbContext.
/// </summary>
public sealed class IamDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public IamDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var conn = config.GetConnectionString("Identity") ?? "Host=localhost;Port=5432;Database=railfactory_iam;Username=railfactory;Password=railfactory_secret";
        var options = new DbContextOptionsBuilder<IamDbContext>().UseNpgsql(conn).Options;
        return new IamDbContext(options);
    }
}
