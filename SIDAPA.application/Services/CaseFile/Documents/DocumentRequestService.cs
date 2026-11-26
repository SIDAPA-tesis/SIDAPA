using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.application.DTOs.Core.Documents;
using SIDAPA.application.Interface.CaseFile.Documents;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Interface.Core.Persistence;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CaseFile;
using SIDAPA.domain.Rules.CaseFile;
using SIDAPA.domain.Rules.Core;
using static SIDAPA.domain.Enums.Core.ReferenciaStatusEnum;

namespace SIDAPA.application.Services.CaseFile.Documents;

/// <summary>
/// HU-05 · "Aviso de regreso al Capturador y cambio de estatus" (SCRUM-64): el cliente sube solo el documento
/// que le pidieron, se agrega al expediente, la referencia regresa a "En revisión" cuando ya no queda ninguna
/// solicitud abierta y se avisa al Capturador que pidió el documento.
/// </summary>
public class DocumentRequestService(
    IDocumentRequestRepository requests,
    IReferenciaRepository referencias,
    IDocumentRepository documents,
    IDocumentService documentService,
    IUnitOfWork unitOfWork,
    INotificationService notifications,
    TimeProvider clock) : IDocumentRequestService
{
    private const string HistoryComment = "El cliente entregó los documentos solicitados.";

    public Task<IReadOnlyList<PendingDocumentRequestDto>> GetPendingAsync(int clientId, CancellationToken ct = default) =>
        requests.GetOpenByClientAsync(clientId, ct);

    public async Task<DocumentRequestDetailDto> GetForUploadAsync(int requestId, int clientId, CancellationToken ct = default)
    {
        var request = await requests.GetForClientAsync(requestId, clientId, ct)
            ?? throw new DocumentRequestNotFoundException();

        DocumentRequestRule.EnsureCanAttend(request.ReferenciaStatus, request.IsAttended);
        return request;
    }

    public async Task<DocumentRequestAttendedDto> AttendAsync(AttendDocumentRequestDto attend, CancellationToken ct = default)
    {
        // 1. La solicitud se busca SIEMPRE junto con el cliente del token: otra empresa no puede ni verla.
        var request = await GetForUploadAsync(attend.RequestId, attend.ClientId, ct);

        // 2. Archivo al bucket, fuera de la transacción. El tipo sale de la solicitud, nunca del formulario.
        var stored = await documentService.StoreFileAsync(new UploadDocumentDto(
            request.ReferenciaId, request.DocumentType, attend.FileName, attend.SizeInBytes, attend.Content, attend.UserId), ct);

        // 3. Documento + cierre de la solicitud + estatus: todo o nada.
        int remaining;
        bool returnedToReview;
        try
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

            // El candado hace que dos subidas de la misma referencia vayan una tras otra. Sin él, cada una
            // vería abierta la solicitud de la otra y la referencia se quedaría en espera para siempre.
            var status = await referencias.LockStatusAsync(request.ReferenciaId, ct)
                ?? throw new DocumentRequestNotFoundException();
            DocumentRequestRule.EnsureCanAttend(status, alreadyAttended: false);

            var document = new DocumentEntity
            {
                ReferenciaId = request.ReferenciaId,
                DocumentTypeId = (short)request.DocumentType,
                FileName = stored.FileName,
                BucketPath = stored.BucketPath,
                SizeInBytes = (int)stored.SizeInBytes,   // ya validado: máximo 20 MB
                Status = nameof(DocumentStatusEnum.RECIBIDO),
                UploadedByUserId = attend.UserId,
                UploadedAt = stored.UploadedAt
            };
            await documents.AddAsync(document, ct);

            var now = Now();
            if (!await requests.MarkAttendedAsync(request.RequestId, document.Id, now, ct))
                throw new DocumentRequestAlreadyAttendedException();   // doble envío: ganó el primero

            remaining = await requests.CountOpenAsync(request.ReferenciaId, ct);
            returnedToReview = false;
            if (DocumentRequestRule.ShouldReturnToReview(remaining))
            {
                ReferenciaStatusFlow.EnsureCanChange(EN_ESPERA_DE_DOCUMENTOS, EN_REVISION);
                returnedToReview = await referencias.ChangeStatusAsync(
                    request.ReferenciaId, EN_ESPERA_DE_DOCUMENTOS, EN_REVISION, attend.UserId, HistoryComment, now, ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            // La transacción ya se deshizo al salir del using: solo falta el archivo del bucket.
            await documentService.DeleteFileQuietlyAsync(stored.BucketPath);
            throw;
        }

        // 4. Aviso al Capturador que lo pidió, ya con todo guardado.
        await notifications.NotifyUserAsync(
            request.RequestedByUserId,
            CapturadorMessage(request, remaining, returnedToReview),
            request.ReferenciaId,
            ct: ct);

        return new DocumentRequestAttendedDto(request.ReferenciaNumber, request.DocumentTypeName, remaining, returnedToReview);
    }

    private static string CapturadorMessage(DocumentRequestDetailDto request, int remaining, bool returnedToReview)
    {
        var uploaded = $"El cliente subió \"{request.DocumentTypeName}\" de la referencia {request.ReferenciaNumber}.";

        if (returnedToReview)
            return $"{uploaded} Ya no hay documentos pendientes: la referencia regresó a En revisión.";

        return remaining switch
        {
            0 => $"{uploaded} Ya no hay documentos pendientes.",
            1 => $"{uploaded} Falta 1 documento por recibir.",
            _ => $"{uploaded} Faltan {remaining} documentos por recibir."
        };
    }

    // UTC sin zona: las columnas son "timestamp without time zone" (igual que en ArrivalNoticeService).
    private DateTime Now() => DateTime.SpecifyKind(clock.GetUtcNow().UtcDateTime, DateTimeKind.Unspecified);
}
