using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.application.Interface.CaseFile.Documents;
using SIDAPA.application.Interface.CaseFile.Expediente;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Interface.Core.Persistence;
using SIDAPA.application.Interface.External.CloudStorage;
using SIDAPA.domain.Enums.CaseFile;
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.CaseFile;
using SIDAPA.domain.Rules.Core;
using static SIDAPA.domain.Enums.Core.ReferenciaStatusEnum;

namespace SIDAPA.application.Services.CaseFile.Expediente;

public class ExpedienteReviewService(
    IExpedienteRepository expedientes,
    IReferenciaRepository referencias,
    IDocumentRepository documents,
    IUnitOfWork unitOfWork,
    IExpedienteService expediente,
    ICloudStorageService cloudStorage,
    ISpreadsheetPreviewReader spreadsheets,
    TimeProvider clock) : IExpedienteReviewService
{
    public const int PageSize = 20;
    private const string StartReviewComment = "El Capturador comenzó a revisar el expediente.";

    public Task<ExpedientePageDto> SearchAsync(ExpedienteFilterDto filter, CancellationToken ct = default) =>
        expedientes.SearchAsync(filter with { Query = filter.Query?.Trim(), Page = Math.Max(1, filter.Page) }, PageSize, ct);

    public async Task<ExpedienteReviewDto?> GetAsync(int referenciaId, CancellationToken ct = default)
    {
        var detail = await expedientes.GetDetailAsync(referenciaId, ct);
        if (detail is null)
            return null;

        var received = detail.Documents.Select(d => (d.Type, d.Status)).ToList();
        var checklist = detail.RequiredDocuments
            .Select(r => new ChecklistItemDto(r.Type, r.Name, r.IsMandatory, ExpedienteRule.ChecklistStatus(r.Type, received)))
            .ToList();

        return new ExpedienteReviewDto(detail, checklist, DocumentReviewRule.CanReview(detail.Status));
    }

    public async Task<DocumentPreviewDto?> GetPreviewAsync(int documentId, CancellationToken ct = default)
    {
        var document = await documents.GetByIdAsync(documentId, ct);
        if (document is null)
            return null;

        var kind = DocumentFileRule.PreviewKindFor(document.FileName);
        if (kind != DocumentPreviewKindEnum.Spreadsheet)
            return new DocumentPreviewDto(document.Id, document.ReferenciaId, document.FileName, kind, null);

        var download = await cloudStorage.DownloadAsync(document.BucketPath, ct);
        await using var content = download.Content;
        return new DocumentPreviewDto(document.Id, document.ReferenciaId, document.FileName, kind, spreadsheets.Read(content));
    }

    public async Task<DocumentFileDto?> OpenFileAsync(int documentId, CancellationToken ct = default)
    {
        var document = await documents.GetByIdAsync(documentId, ct);
        if (document is null)
            return null;

        var kind = DocumentFileRule.PreviewKindFor(document.FileName);
        if (kind is not (DocumentPreviewKindEnum.Pdf or DocumentPreviewKindEnum.Text))
            return null;

        var download = await cloudStorage.DownloadAsync(document.BucketPath, ct);
        return new DocumentFileDto(download.Content, kind, document.FileName);
    }

    public async Task<ExpedienteStatusDto> MarkAsync(ReviewDocumentDto review, CancellationToken ct = default)
    {
        DocumentReviewRule.EnsureValidMark(review.Status);
        var note = DocumentReviewRule.NormalizeNote(review.Status, review.Note);

        await using (var transaction = await unitOfWork.BeginTransactionAsync(ct))
        {
            // Mismo candado que la subida de documentos solicitados (HU-05): dos Capturadores marcando la misma
            // referencia van uno tras otro, y nadie marca sobre una referencia que acaba de cambiar de estatus.
            var status = await referencias.LockStatusAsync(review.ReferenciaId, ct)
                ?? throw new ReferenciaNotFoundException(review.ReferenciaId);
            DocumentReviewRule.EnsureCanReview(status);

            if (DocumentReviewRule.StartsReview(status))
            {
                ReferenciaStatusFlow.EnsureCanChange(RECIBIDA, EN_REVISION);
                await referencias.ChangeStatusAsync(review.ReferenciaId, RECIBIDA, EN_REVISION, review.UserId,
                    StartReviewComment, Now(), ct);
            }

            if (!await documents.UpdateStatusAsync(review.DocumentId, review.ReferenciaId, review.Status, note, ct))
                throw new DocumentNotFoundException();

            await referencias.AssignCapturadorAsync(review.ReferenciaId, review.UserId, ct);

            await transaction.CommitAsync(ct);
        }

        // Fuera de la transacción: EvaluateAsync abre la suya y avisa al cliente ya con todo guardado.
        return await expediente.EvaluateAsync(review.ReferenciaId, review.UserId, ct);
    }

    // UTC sin zona: las columnas son "timestamp without time zone".
    private DateTime Now() => DateTime.SpecifyKind(clock.GetUtcNow().UtcDateTime, DateTimeKind.Unspecified);
}
