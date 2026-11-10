using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.presentation;

/// <summary>
/// Used only by `dotnet ef` design-time tooling, so migrations can be generated without booting
/// the full web host (which also requires Jwt/GoogleCloudStorage/TaxScan configuration).
/// </summary>
public class SidapaDbContextFactory : IDesignTimeDbContextFactory<SidapaDbContext>
{
    public SidapaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Missing 'ConnectionStrings:DefaultConnection' for design-time DbContext creation.");

        var optionsBuilder = new DbContextOptionsBuilder<SidapaDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SidapaDbContext.Schema));

        return new SidapaDbContext(optionsBuilder.Options);
    }
}
