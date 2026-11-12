using SIDAPA.application.DTOs.Core.Documents;

namespace SIDAPA.application.Interface.Core;

/// <summary>Guarda los documentos del expediente en la nube y registra sus datos (SCRUM-48).</summary>
public interface IDocumentService
{
    /// <summary>Valida el archivo, lo sube al bucket con nombre normalizado y lo registra en ope_documento.</summary>
    Task<DocumentDto> UploadAsync(UploadDocumentDto upload, CancellationToken ct = default);

    Task<IReadOnlyList<DocumentDto>> GetByReferenciaAsync(int referenciaId, CancellationToken ct = default);

    /// <summary>Link temporal para ver o descargar el archivo sin hacer público el bucket.</summary>
    Task<string> GetDownloadUrlAsync(int documentId, CancellationToken ct = default);
}