using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Catalogs;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Repositories.CaseFile.Documents;

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
        await SeedDemoDocumentRequestsAsync(context, ct);
    }

    /// <summary>
    /// HU-05 (SCRUM-64): mientras no exista HU-04 (el Capturador pide documentos), deja una referencia demo
    /// en "En espera de documentos" con dos solicitudes abiertas para probar la subida desde el portal del cliente.
    /// Las solicitudes se crean con el mismo repositorio que usará HU-04. Solo Development.
    /// </summary>
    private static async Task SeedDemoDocumentRequestsAsync(SidapaDbContext context, CancellationToken ct)
    {
        const string referenciaNumber = "DEMO-HU05";
        if (await context.Referencias.AnyAsync(r => r.ReferenciaNumber == referenciaNumber, ct))
            return;

        var cliente = await context.Users.FirstOrDefaultAsync(u => u.Email == "cliente@sidapa.local", ct);
        var capturador = await context.Users.FirstOrDefaultAsync(u => u.Email == "capturador@sidapa.local", ct);
        if (cliente?.ClientId is not { } clientId || capturador is null)
            return;

        var receivedAt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-2), DateTimeKind.Unspecified);
        var requestedAt = receivedAt.AddDays(1);

        var referencia = new ReferenciaEntity
        {
            ReferenciaNumber = referenciaNumber,
            ClientId = clientId,
            OperationTypeCode = (short)OperationTypeEnum.IMPORTACION,
            AduanaCode = "470",   // AICM, sembrada por la migración AvisoArribo
            StatusId = (short)ReferenciaStatusEnum.EN_ESPERA_DE_DOCUMENTOS,
            CreatedAt = receivedAt
        };
        referencia.StatusHistories.Add(History(ReferenciaStatusEnum.RECIBIDA, cliente.Id, receivedAt, "Aviso de arribo recibido"));
        referencia.StatusHistories.Add(History(ReferenciaStatusEnum.EN_REVISION, capturador.Id, receivedAt.AddHours(2), "Datos de demostración"));
        referencia.StatusHistories.Add(History(ReferenciaStatusEnum.EN_ESPERA_DE_DOCUMENTOS, capturador.Id, requestedAt, "Datos de demostración: se pidieron documentos"));
        context.Referencias.Add(referencia);
        await context.SaveChangesAsync(ct);

        var requests = new DocumentRequestRepository(context);
        await requests.CreateAsync(new DocumentRequestEntity
        {
            ReferenciaId = referencia.Id,
            DocumentTypeId = (short)DocumentTypeEnum.DOCUMENTO_TRANSPORTE,
            Note = "El B/L que enviaste no es legible. Súbelo de nuevo, por favor.",
            RequestedByUserId = capturador.Id,
            RequestedAt = requestedAt
        }, ct);
        await requests.CreateAsync(new DocumentRequestEntity
        {
            ReferenciaId = referencia.Id,
            DocumentTypeId = (short)DocumentTypeEnum.LISTA_EMPAQUE,
            Note = "Falta la lista de empaque.",
            RequestedByUserId = capturador.Id,
            RequestedAt = requestedAt
        }, ct);
    }

    private static StatusHistoryEntity History(ReferenciaStatusEnum status, int userId, DateTime at, string comment) =>
        new() { StatusId = (short)status, UserId = userId, ChangedAt = at, Comment = comment };

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
