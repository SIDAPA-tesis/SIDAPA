using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.test.Persistence;

/// <summary>
/// Crea filas mínimas y válidas en la base de Testcontainers. Todo lleva un sufijo único para que las
/// pruebas no choquen entre sí aunque compartan la base (correos, RFC y números de referencia son únicos).
/// </summary>
public static class TestData
{
    // 10 caracteres hexadecimales: caben en el RFC (13) y en el número de referencia (20).
    private static string Unique() => Guid.NewGuid().ToString("N")[..10];

    public static async Task<UserEntity> UserAsync(SidapaDbContext context, RolesEnum role = RolesEnum.CAPTURADOR,
        int? clientId = null, bool isActive = true)
    {
        var user = new UserEntity
        {
            Name = "Usuario de prueba",
            Email = $"usuario-{Unique()}@sidapa.test",
            PasswordHash = "hash-no-usado",
            RoleId = (short)role,
            ClientId = clientId,
            IsActive = isActive
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public static async Task<ClientEntity> ClientAsync(SidapaDbContext context)
    {
        var suffix = Unique();
        var client = new ClientEntity
        {
            Rfc = $"T{suffix}",
            BusinessName = $"Cliente de prueba {suffix}",
            Address = "Domicilio de prueba",
            NotificationEmail = $"cliente-{suffix}@sidapa.test"
        };
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        return client;
    }

    /// <summary>Referencia de importación en la aduana 470 (AICM, sembrada por la migración AvisoArribo).</summary>
    public static async Task<ReferenciaEntity> ReferenciaAsync(SidapaDbContext context, int clientId,
        ReferenciaStatusEnum status = ReferenciaStatusEnum.RECIBIDA, OperationTypeEnum operation = OperationTypeEnum.IMPORTACION)
    {
        var referencia = new ReferenciaEntity
        {
            ReferenciaNumber = $"T-{Unique()}",
            ClientId = clientId,
            OperationTypeCode = (short)operation,
            AduanaCode = "470",
            StatusId = (short)status,
            CreatedAt = new DateTime(2026, 11, 23, 10, 0, 0)
        };
        context.Referencias.Add(referencia);
        await context.SaveChangesAsync();
        return referencia;
    }

    /// <summary>Documento ya recibido (fila en ope_documento; no sube nada al bucket).</summary>
    public static async Task<DocumentEntity> DocumentAsync(SidapaDbContext context, int referenciaId, int userId,
        DocumentTypeEnum type = DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR)
    {
        var suffix = Unique();
        var document = new DocumentEntity
        {
            ReferenciaId = referenciaId,
            DocumentTypeId = (short)type,
            FileName = $"documento-{suffix}.pdf",
            BucketPath = $"referencias/{referenciaId}/documento-{suffix}.pdf",
            SizeInBytes = 1024,
            Status = nameof(DocumentStatusEnum.RECIBIDO),
            UploadedByUserId = userId,
            UploadedAt = new DateTime(2026, 11, 23, 10, 5, 0)
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        return document;
    }

    /// <summary>Solicitud de documento faltante (ope_solicitud_documento), abierta salvo que se diga lo contrario.</summary>
    public static async Task<DocumentRequestEntity> DocumentRequestAsync(SidapaDbContext context, int referenciaId,
        int capturadorId, DocumentTypeEnum type = DocumentTypeEnum.DOCUMENTO_TRANSPORTE, DateTime? attendedAt = null)
    {
        var request = new DocumentRequestEntity
        {
            ReferenciaId = referenciaId,
            DocumentTypeId = (short)type,
            Note = "Nota de prueba",
            RequestedByUserId = capturadorId,
            RequestedAt = new DateTime(2026, 11, 23, 11, 0, 0),
            AttendedAt = attendedAt
        };
        context.DocumentRequests.Add(request);
        await context.SaveChangesAsync();
        return request;
    }

    /// <summary>Registro previo en ope_bitacora_estatus.</summary>
    public static async Task<StatusHistoryEntity> StatusHistoryAsync(SidapaDbContext context, int referenciaId,
        int userId, ReferenciaStatusEnum status = ReferenciaStatusEnum.RECIBIDA)
    {
        var history = new StatusHistoryEntity
        {
            ReferenciaId = referenciaId,
            StatusId = (short)status,
            UserId = userId,
            ChangedAt = new DateTime(2026, 11, 23, 10, 0, 0),
            Comment = "Registro de prueba"
        };
        context.StatusHistories.Add(history);
        await context.SaveChangesAsync();
        return history;
    }
}
