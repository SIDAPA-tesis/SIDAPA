using SIDAPA.application.DTOs.Core.Documents;
using SIDAPA.application.DTOs.External.CloudStorage;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Interface.External.CloudStorage;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.application.Services.Core;

public class DocumentService(
    IDocumentRepository documentRepository,
    IDocumentFileValidator fileValidator,
    ICloudStorageService cloudStorage) : IDocumentService
{
    public async Task<DocumentDto> UploadAsync(UploadDocumentDto upload, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(upload.DocumentType))
            throw new InvalidDocumentTypeException();

        if (!upload.Content.CanSeek)
            throw new ArgumentException("El contenido debe permitir Seek.", nameof(upload));

        var referenciaNumber = await documentRepository.GetReferenciaNumberAsync(upload.ReferenciaId, ct)
            ?? throw new ReferenciaNotFoundException(upload.ReferenciaId);

        // SCRUM-50: tipo, tamaño y contenido. El validador lee los primeros bytes,
        // por eso después se regresa el Stream al inicio para subir el archivo completo.
        await fileValidator.ValidateAsync(upload.FileName, upload.SizeInBytes, upload.Content, ct);
        upload.Content.Position = 0;

        var now = UtcNow();
        var bucketPath = DocumentPathRule.Build(referenciaNumber, upload.DocumentType, upload.FileName, now, Guid.NewGuid());
        var contentType = DocumentFileRule.ContentTypeFor(upload.FileName);

        await cloudStorage.UploadAsync(new CloudStorageUploadRequest(upload.Content, bucketPath, contentType), ct);

        var document = new DocumentEntity
        {
            ReferenciaId = upload.ReferenciaId,
            DocumentTypeId = (short)upload.DocumentType,
            FileName = DocumentPathRule.OriginalFileName(upload.FileName),
            BucketPath = bucketPath,
            SizeInBytes = (int)upload.SizeInBytes,   // ya validado: máximo 20 MB
            Status = nameof(DocumentStatusEnum.RECIBIDO),
            UploadedByUserId = upload.UploadedByUserId,
            UploadedAt = now
        };

        try
        {
            await documentRepository.AddAsync(document, ct);
        }
        catch
        {
            // Si no se pudo registrar en la BD, se borra del bucket para no dejar archivos sin dueño.
            await DeleteQuietlyAsync(bucketPath);
            throw;
        }

        return ToDto(document);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetByReferenciaAsync(int referenciaId, CancellationToken ct = default)
    {
        var documents = await documentRepository.GetByReferenciaAsync(referenciaId, ct);
        return documents.Select(ToDto).ToList();
    }

    public async Task<string> GetDownloadUrlAsync(int documentId, CancellationToken ct = default)
    {
        var document = await documentRepository.GetByIdAsync(documentId, ct)
            ?? throw new DocumentNotFoundException();

        return await cloudStorage.GenerateSignedUrlAsync(document.BucketPath, ct: ct);
    }

    // Si también falla el borrado, se deja pasar el error original (el de la BD), que es el importante.
    private async Task DeleteQuietlyAsync(string bucketPath)
    {
        try
        {
            await cloudStorage.DeleteAsync(bucketPath, CancellationToken.None);
        }
        catch
        {
            // sin acción: el archivo queda huérfano en el bucket, pero no se oculta el error real
        }
    }

    private static DocumentDto ToDto(DocumentEntity d) => new(
        d.Id,
        d.ReferenciaId,
        (DocumentTypeEnum)d.DocumentTypeId,
        d.FileName,
        d.BucketPath,
        d.SizeInBytes,
        Enum.Parse<DocumentStatusEnum>(d.Status, ignoreCase: true),
        d.UploadedByUserId,
        d.UploadedAt);

    // Igual que en UserService: la columna es "timestamp without time zone".
    private static DateTime UtcNow() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}