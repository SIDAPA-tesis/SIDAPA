using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.Interface.Core;

public interface IDocumentRepository
{
    /// <summary>Número de la referencia (p. ej. R-000123) o null si no existe.</summary>
    Task<string?> GetReferenciaNumberAsync(int referenciaId, CancellationToken ct = default);
    Task AddAsync(DocumentEntity document, CancellationToken ct = default);
    Task<DocumentEntity?> GetByIdAsync(int documentId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentEntity>> GetByReferenciaAsync(int referenciaId, CancellationToken ct = default);

    /// <summary>false si el documento no existe o es de otra referencia.</summary>
    Task<bool> UpdateStatusAsync(int documentId, int referenciaId, DocumentStatusEnum status, CancellationToken ct = default);
}