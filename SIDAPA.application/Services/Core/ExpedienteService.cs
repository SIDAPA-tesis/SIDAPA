using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;
using static SIDAPA.domain.Enums.Core.ReferenciaStatusEnum;

namespace SIDAPA.application.Services.Core;

public class ExpedienteService(
    IReferenciaRepository referencias,
    IDocumentRepository documents,
    INotificationService notifications) : IExpedienteService
{
    private const string HistoryComment = "Cambio automático: todos los documentos obligatorios están correctos.";

    public async Task<ExpedienteStatusDto> CheckAsync(int referenciaId, CancellationToken ct = default)
    {
        var (_, status) = await BuildAsync(referenciaId, ct);
        return status;
    }

    public async Task<ExpedienteStatusDto> EvaluateAsync(int referenciaId, int userId, CancellationToken ct = default)
    {
        var (referencia, status) = await BuildAsync(referenciaId, ct);

        // Solo cambia sola cuando el Capturador la está revisando; en cualquier otro estatus no se toca.
        if (!status.IsComplete || status.Status != EN_REVISION)
            return status;

        ReferenciaStatusFlow.EnsureCanChange(EN_REVISION, EXPEDIENTE_COMPLETO);

        var changed = await referencias.ChangeStatusAsync(
            referenciaId, EN_REVISION, EXPEDIENTE_COMPLETO, userId, HistoryComment, UtcNow(), ct);
        if (!changed)   // otro usuario la cambió al mismo tiempo
            return status;

        await notifications.NotifyClientAsync(
            referencia.ClientId,
            $"El expediente de tu referencia {referencia.ReferenciaNumber} está completo. Ya comenzamos con tu pedimento.",
            referenciaId,
            ct: ct);

        return status with { Status = EXPEDIENTE_COMPLETO, StatusChanged = true };
    }

    private async Task<(ReferenciaEntity Referencia, ExpedienteStatusDto Status)> BuildAsync(int referenciaId, CancellationToken ct)
    {
        var referencia = await referencias.GetByIdAsync(referenciaId, ct)
            ?? throw new ReferenciaNotFoundException(referenciaId);

        var mandatory = (await referencias.GetMandatoryDocumentTypeIdsAsync(referencia.OperationTypeCode, ct))
            .Select(id => (DocumentTypeEnum)id)
            .ToList();

        var received = (await documents.GetByReferenciaAsync(referenciaId, ct))
            .Select(d => ((DocumentTypeEnum)d.DocumentTypeId, Enum.Parse<DocumentStatusEnum>(d.Status, ignoreCase: true)));

        var missing = ExpedienteRule.MissingDocuments(mandatory, received);

        var status = new ExpedienteStatusDto(
            referenciaId,
            (ReferenciaStatusEnum)referencia.StatusId,
            mandatory,
            missing,
            ExpedienteRule.IsComplete(mandatory, missing),
            StatusChanged: false);

        return (referencia, status);
    }

    // Igual que en UserService: las columnas son "timestamp without time zone".
    private static DateTime UtcNow() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}