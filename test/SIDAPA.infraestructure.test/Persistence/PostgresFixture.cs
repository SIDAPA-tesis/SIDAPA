using Microsoft.EntityFrameworkCore;
using SIDAPA.domain.Entities.Core.Catalogs;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Persistence;
using Testcontainers.PostgreSql;

namespace SIDAPA.infraestructure.test.Persistence;

/// <summary>
/// PostgreSQL 16 real en Docker (Testcontainers) con TODAS las migraciones de EF aplicadas y el catálogo de roles.
/// Se levanta una sola vez por corrida para todas las clases de la colección <see cref="PostgresCollection"/>.
/// <para>
/// Las pruebas comparten la base: cada prueba crea sus propios datos con <see cref="TestData"/> (ids y correos
/// únicos) y nunca asume que una tabla está vacía.
/// </para>
/// <para>
/// Sin Docker las pruebas se marcan como omitidas (local). En CI define <c>SIDAPA_REQUIRE_DOCKER=1</c> para que
/// fallen en lugar de omitirse.
/// </para>
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string? UnavailableReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            UnavailableReason = $"Docker no está disponible para Testcontainers: {ex.GetBaseException().Message}";
            return;
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        // Los roles no vienen en las migraciones (los crea DatabaseSeeder al arrancar la app).
        foreach (var role in Enum.GetValues<RolesEnum>())
            context.Roles.Add(new RoleEntity { Id = (short)role, Name = role.ToString() });
        await context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    /// <summary>Contexto nuevo (cada repositorio bajo prueba debe recibir el suyo, como en una petición HTTP).</summary>
    public SidapaDbContext CreateContext()
    {
        EnsureAvailable();
        var options = new DbContextOptionsBuilder<SidapaDbContext>()
            .UseNpgsql(_container!.GetConnectionString(), npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SidapaDbContext.Schema))
            .Options;
        return new SidapaDbContext(options);
    }

    /// <summary>Omite la prueba si no hay Docker (o la hace fallar si <c>SIDAPA_REQUIRE_DOCKER=1</c>).</summary>
    public void EnsureAvailable()
    {
        if (UnavailableReason is null)
            return;

        if (Environment.GetEnvironmentVariable("SIDAPA_REQUIRE_DOCKER") == "1")
            Assert.Fail(UnavailableReason);

        Assert.Skip(UnavailableReason);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
