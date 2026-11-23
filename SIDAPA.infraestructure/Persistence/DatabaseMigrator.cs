using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SIDAPA.infraestructure.Persistence;

/// <summary>
/// Aplica las migraciones pendientes de EF Core al arrancar (equivale a <c>dotnet ef database update</c>).
/// Se usa en Docker Compose (<c>Database:MigrateOnStartup = true</c>) para que la base quede lista sin pasos manuales.
/// Es idempotente: si no hay migraciones pendientes no hace nada, y EF toma un bloqueo en la base
/// para que dos instancias no migren a la vez.
/// </summary>
public static class DatabaseMigrator
{
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SidapaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseMigrator));

        var pending = (await context.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Base de datos al día: no hay migraciones pendientes.");
            return;
        }

        logger.LogInformation("Aplicando {Count} migración(es) pendiente(s): {Migrations}",
            pending.Count, string.Join(", ", pending));
        await context.Database.MigrateAsync(ct);
        logger.LogInformation("Migraciones aplicadas.");
    }
}
