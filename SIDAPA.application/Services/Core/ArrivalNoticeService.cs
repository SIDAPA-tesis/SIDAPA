using SIDAPA.application.DTOs.Core.Documents;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Interface.External.CloudStorage;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.application.Services.Core;

/// <summary>Aviso de arribo del Cliente (HU-02 / SCRUM-46).</summary>
public class ArrivalNoticeService(
    IReferenciaRepository referencias,
    ICatalogRepository catalogs,
    IDocumentFileValidator fileValidator,
    IDocumentService documents,
    ICloudStorageService cloudStorage,
    TimeProvider clock) : IArrivalNoticeService
{
    public async Task<ArrivalNoticeCreatedDto> CreateAsync(CreateArrivalNoticeDto notice, CancellationToken ct = default)
    {
        // 1. Todo se valida antes de crear nada: un .exe en el segundo archivo no deja una referencia a medias.
        ArrivalNoticeRule.EnsureValid(notice.OperationType, notice.ArrivalDate, notice.Notes, notice.Files.Count);

        var aduanaCode = notice.AduanaCode?.Trim();
        if (string.IsNullOrEmpty(aduanaCode) || !await catalogs.IsActiveAduanaAsync(aduanaCode, ct))
            throw new AduanaNotFoundException(aduanaCode);

        foreach (var file in notice.Files)
            await ValidateFileAsync(file, ct);

        // 2. Referencia en RECIBIDA con su número único (secuencia de la BD) y su bitácora.
        var referencia = new ReferenciaEntity
        {
            ReferenciaNumber = ReferenciaNumberRule.Format(await referencias.NextNumberAsync(ct)),
            ClientId = notice.ClientId,
            OperationTypeCode = (short)notice.OperationType,
            AduanaCode = aduanaCode,
            ArrivalDate = notice.ArrivalDate,
            Notes = string.IsNullOrWhiteSpace(notice.Notes) ? null : notice.Notes.Trim(),
            StatusId = (short)ReferenciaStatusEnum.RECIBIDA,
            CreatedAt = Now()
        };
        await referencias.AddAsync(referencia, notice.UserId, ct);

        // 3. Documentos al bucket + ope_documento. Si uno falla, se deshace todo el aviso.
        var uploadedPaths = new List<string>();
        try
        {
            foreach (var file in notice.Files)
            {
                var document = await documents.UploadAsync(new UploadDocumentDto(
                    referencia.Id, file.DocumentType, file.FileName, file.SizeInBytes, file.Content, notice.UserId), ct);
                uploadedPaths.Add(document.BucketPath);
            }
        }
        catch
        {
            await UndoAsync(referencia.Id, uploadedPaths);
            throw;
        }

        return new ArrivalNoticeCreatedDto(referencia.Id, referencia.ReferenciaNumber, uploadedPaths.Count);
    }

    public Task<IReadOnlyList<ClientReferenciaDto>> GetClientPanelAsync(int clientId, CancellationToken ct = default) =>
        referencias.GetByClientAsync(clientId, ct);

    public async Task<ArrivalNoticeCatalogsDto> GetCatalogsAsync(CancellationToken ct = default) =>
        new(await catalogs.GetActiveAduanasAsync(ct), await catalogs.GetDocumentTypesAsync(ct));

    private async Task ValidateFileAsync(ArrivalNoticeFileDto file, CancellationToken ct)
    {
        if (!Enum.IsDefined(file.DocumentType))
            throw new InvalidDocumentTypeException();

        if (!file.Content.CanSeek)
            throw new ArgumentException("El contenido debe permitir Seek.", nameof(file));

        await fileValidator.ValidateAsync(file.FileName, file.SizeInBytes, file.Content, ct);
        file.Content.Position = 0;   // el validador leyó el encabezado; se sube completo después
    }

    // Si la compensación también falla se deja pasar el error original, que es el que importa.
    private async Task UndoAsync(int referenciaId, IEnumerable<string> uploadedPaths)
    {
        foreach (var path in uploadedPaths)
        {
            try { await cloudStorage.DeleteAsync(path, CancellationToken.None); }
            catch { /* queda huérfano en el bucket; no se oculta el error real */ }
        }

        try { await referencias.DeleteAsync(referenciaId, CancellationToken.None); }
        catch { /* igual que arriba */ }
    }

    // UTC sin zona: la columna es "timestamp without time zone" (igual que en UserService/DocumentService).
    private DateTime Now() => DateTime.SpecifyKind(clock.GetUtcNow().UtcDateTime, DateTimeKind.Unspecified);
}
