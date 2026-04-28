using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Imedto.Backend.Infrastructure.Database;

/// <summary>
/// Factory usada pelo tooling do EF Core (dotnet ef migrations add / database update).
/// Lê a connection string "Migrations" (pooler session mode, porta 5432) do appsettings
/// do projeto API, para não depender do Host configurado em Program.cs.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // baseDir = Imedto.Backend.Infrastructure/bin/Debug/net10.0/ → sobe 4 até Services/ e entra em Imedto.Backend.API
        var apiProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Imedto.Backend.API"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Migrations")
            ?? configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Nem ConnectionStrings:Migrations nem ConnectionStrings:Default configurados em appsettings do API project.");

        var builder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"));

        return new AppDbContext(builder.Options);
    }
}
