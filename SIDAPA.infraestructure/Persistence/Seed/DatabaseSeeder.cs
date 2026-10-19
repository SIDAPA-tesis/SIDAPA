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
/// <c>&lt;rol&gt;@sidapa.local</c>. El rol Cliente no se crea porque necesita una empresa.</item>
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
    }

    /// <summary>CAPTURADOR → "Capturador", para mostrarlo en pantalla.</summary>
    private static string DisplayName(RolesEnum role) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(role.ToString().ToLowerInvariant());
}
