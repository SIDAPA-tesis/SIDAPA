using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Catalogs;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.infraestructure.Persistence.Seed;

/// <summary>
/// Datos mínimos para poder entrar al sistema:
/// <list type="bullet">
/// <item>Catálogo de roles (cat_rol) a partir de <see cref="RolesEnum"/>, siempre que falte alguno.</item>
/// <item>Solo si se pasa una contraseña (Development): un usuario de prueba por rol interno,
/// <c>&lt;rol&gt;@sidapa.local</c>, más una empresa demo con su usuario Cliente (<c>cliente@sidapa.local</c>).</item>
/// </list>
/// Es idempotente: no duplica ni modifica lo que ya existe.
/// TODO: pasar los roles a HasData en RoleConfiguration con su migración.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, string? devUsersPassword, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SidapaDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var roles = Enum.GetValues<RolesEnum>();

        var existingRoles = await context.Roles.Select(r => r.Id).ToListAsync(ct);
        foreach (var role in roles.Where(r => !existingRoles.Contains((short)r)))
            context.Roles.Add(new RoleEntity { Id = (short)role, Name = DisplayName(role) });
        await context.SaveChangesAsync(ct);

        if (string.IsNullOrWhiteSpace(devUsersPassword))
            return;

        foreach (var role in roles.Where(r => r != RolesEnum.CLIENTE))
        {
            var email = $"{role.ToString().ToLowerInvariant()}@sidapa.local";
            if (await context.Users.AnyAsync(u => u.Email == email, ct))
                continue;

            context.Users.Add(new UserEntity
            {
                Name = $"{DisplayName(role)} de prueba",
                Email = email,
                PasswordHash = hasher.Hash(devUsersPassword),
                RoleId = (short)role,
                IsActive = true
            });
        }
        await context.SaveChangesAsync(ct);

        await SeedDemoClientAsync(context, hasher, devUsersPassword, ct);
    }

    /// <summary>Empresa demo y su usuario Cliente (HU-02). Solo Development: llega aquí únicamente con contraseña.</summary>
    private static async Task SeedDemoClientAsync(SidapaDbContext context, IPasswordHasher hasher, string password, CancellationToken ct)
    {
        const string email = "cliente@sidapa.local";
        if (await context.Users.AnyAsync(u => u.Email == email, ct))
            return;

        const string rfc = "EDE010101AA1";
        var client = await context.Clients.FirstOrDefaultAsync(c => c.Rfc == rfc, ct);
        if (client is null)
        {
            client = new ClientEntity
            {
                Rfc = rfc,
                BusinessName = "Empresa Demo S.A. de C.V.",
                Address = "Domicilio de prueba",
                NotificationEmail = email
            };
            context.Clients.Add(client);
        }

        context.Users.Add(new UserEntity
        {
            Name = "Cliente de prueba",
            Email = email,
            PasswordHash = hasher.Hash(password),
            RoleId = (short)RolesEnum.CLIENTE,
            Client = client,
            IsActive = true
        });
        await context.SaveChangesAsync(ct);
    }

    /// <summary>CAPTURADOR → "Capturador", para mostrarlo en pantalla.</summary>
    private static string DisplayName(RolesEnum role) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(role.ToString().ToLowerInvariant());
}
