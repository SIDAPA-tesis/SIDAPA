using SIDAPA.domain.Entities.Core.Operative;

namespace SIDAPA.application.Interface.Core;

public interface IDocumentRepository
{
    /// <summary>Número de la referencia (p. ej. R-000123) o null si no existe.</summary>
    Task<string?> GetReferenciaNumberAsync(int referenciaId, CancellationToken ct = default);
    Task AddAsync(DocumentEntity document, CancellationToken ct = default);
    Task<DocumentEntity?> GetByIdAsync(int documentId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentEntity>> GetByReferenciaAsync(int referenciaId, CancellationToken ct = default);
}