using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.domain.Entities.Core.Operative;

namespace SIDAPA.application.Interface.CaseFile.Documents;

/// <summary>Tabla ope_solicitud_documento: documentos faltantes que se le piden al cliente (HU-04 / HU-05).</summary>
public interface IDocumentRequestRepository
{
    /// <summary>Registra una solicitud nueva. Hoy la usan el seed de desarrollo y las pruebas; HU-04 la usará desde su servicio.</summary>
    Task CreateAsync(DocumentRequestEntity request, CancellationToken ct = default);

    /// <summary>La solicitud solo si es de una referencia de <paramref name="clientId"/>; null en cualquier otro caso.</summary>
    Task<DocumentRequestDetailDto?> GetForClientAsync(int requestId, int clientId, CancellationToken ct = default);

    /// <summary>Solicitudes abiertas de todas las referencias del cliente, de la más antigua a la más reciente.</summary>
    Task<IReadOnlyList<PendingDocumentRequestDto>> GetOpenByClientAsync(int clientId, CancellationToken ct = default);

    /// <summary>
    /// Cierra la solicitud con el documento que subió el cliente, solo si seguía abierta.
    /// false = ya estaba atendida (doble envío o el otro usuario ganó).
    /// </summary>
    Task<bool> MarkAttendedAsync(int requestId, int documentId, DateTime attendedAt, CancellationToken ct = default);

    Task<int> CountOpenAsync(int referenciaId, CancellationToken ct = default);
}
