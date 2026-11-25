using Microsoft.EntityFrameworkCore;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.test.Persistence;

/// <summary>Ayudantes para pruebas de concurrencia contra PostgreSQL real.</summary>
public static class PostgresLocks
{
    /// <summary>
    /// Espera (consultando pg_locks, sin dormir) a que alguna sesión quede bloqueada por un candado de la transacción
    /// de <paramref name="lockHolder"/>. Así el orden "el SUT ya está esperando, ahora confirma el otro" es determinista.
    /// Si <paramref name="sut"/> termina sin bloquearse, deja de esperar (y la prueba lo detecta por su resultado).
    /// Misma técnica que <c>ReferenciaRepositoryTests.WaitUntilSomeoneWaitsForLockOfAsync</c>.
    /// </summary>
    public static async Task WaitUntilSomeoneWaitsForAsync(SidapaDbContext lockHolder, Task sut, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        while (!sut.IsCompleted)
        {
            var blocked = await lockHolder.Database
                .SqlQueryRaw<int>(
                    "SELECT count(*)::int AS \"Value\" FROM pg_locks WHERE NOT granted AND pg_backend_pid() = ANY(pg_blocking_pids(pid))")
                .SingleAsync(timeout.Token);
            if (blocked > 0)
                return;
        }
    }
}
