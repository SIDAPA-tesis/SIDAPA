using SIDAPA.application.DTOs.Core.Documents;

namespace SIDAPA.application.Interface.Core;

/// <summary>Guarda los documentos del expediente en la nube y registra sus datos (SCRUM-48).</summary>
public interface IDocumentService
{
    /// <summary>Valida el archivo, lo sube al bucket con nombre normalizado y lo registra en ope_documento.</summary>
    Task<DocumentDto> UploadAsync(UploadDocumentDto upload, CancellationToken ct = default);

    /// <summary>
    /// Solo valida el archivo y lo sube al bucket con nombre normalizado; NO lo registra en ope_documento.
    /// Para quien registra la fila dentro de su propia transacción (HU-05). Si esa transacción falla,
    /// debe llamar a <see cref="DeleteFileQuietlyAsync"/>.
    /// </summary>
    Task<StoredFileDto> StoreFileAsync(UploadDocumentDto upload, CancellationToken ct = default);

    /// <summary>Borra el archivo del bucket sin lanzar (compensación): si falla, el archivo queda huérfano.</summary>
    Task DeleteFileQuietlyAsync(string bucketPath);

    Task<IReadOnlyList<DocumentDto>> GetByReferenciaAsync(int referenciaId, CancellationToken ct = default);

    /// <summary>Link temporal para ver o descargar el archivo sin hacer público el bucket.</summary>
    Task<string> GetDownloadUrlAsync(int documentId, CancellationToken ct = default);
}