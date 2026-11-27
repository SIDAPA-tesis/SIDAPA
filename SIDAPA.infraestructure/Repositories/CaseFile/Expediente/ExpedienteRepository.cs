using Microsoft.EntityFrameworkCore;
using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.application.Interface.CaseFile.Expediente;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.Repositories.CaseFile.Expediente;

public class ExpedienteRepository(SidapaDbContext context) : IExpedienteRepository
{
    private static readonly short[] PendingReviewStatuses =
        [(short)ReferenciaStatusEnum.RECIBIDA, (short)ReferenciaStatusEnum.EN_REVISION];

    public async Task<IReadOnlyList<ExpedienteSummaryDto>> SearchAsync(ExpedienteFilterDto filter, int limit, CancellationToken ct = default)
    {
        var query = context.Referencias.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var pattern = $"%{EscapeLike(filter.Query)}%";
            query = query.Where(x => EF.Functions.ILike(x.ReferenciaNumber, pattern)
                                     || EF.Functions.ILike(x.Client.BusinessName, pattern));
        }

        if (filter.PendingReviewOnly)
            query = query.Where(x => PendingReviewStatuses.Contains(x.StatusId));

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .Select(x => new ExpedienteSummaryDto(
                x.Id,
                x.ReferenciaNumber,
                x.Client.BusinessName,
                x.Aduana.Name,
                (ReferenciaStatusEnum)x.StatusId,
                x.Status.Name,
                x.CreatedAt,
                x.Documents.Count,
                x.Documents.Count(d => d.Status == nameof(DocumentStatusEnum.RECIBIDO))))
            .ToListAsync(ct);
    }

    public async Task<ExpedienteDetailDto?> GetDetailAsync(int referenciaId, CancellationToken ct = default)
    {
        var header = await context.Referencias
            .AsNoTracking()
            .Where(x => x.Id == referenciaId)
            .Select(x => new
            {
                x.Id,
                x.ReferenciaNumber,
                ClientName = x.Client.BusinessName,
                AduanaName = x.Aduana.Name,
                OperationTypeName = x.OperationType.Description,
                x.OperationTypeCode,
                x.StatusId,
                StatusName = x.Status.Name,
                x.ArrivalDate,
                x.Notes,
                x.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
        if (header is null)
            return null;

        var documents = await context.Documents
            .AsNoTracking()
            .Where(x => x.ReferenciaId == referenciaId)
            .OrderBy(x => x.DocumentTypeId)
            .ThenByDescending(x => x.UploadedAt)
            .Select(x => new
            {
                x.Id, x.DocumentTypeId, TypeName = x.DocumentType.Name, x.FileName, x.SizeInBytes, x.Status,
                UploadedByName = x.UploadedByUser.Name, x.UploadedAt
            })
            .ToListAsync(ct);

        // Mismo criterio que ReferenciaRepository.GetMandatoryDocumentTypeIdsAsync: vacío = aplica a todas las claves.
        var required = await context.RequiredDocuments
            .AsNoTracking()
            .Where(x => x.OperationTypeCode == header.OperationTypeCode
                        && (x.PedimentoKeyCode == null || x.PedimentoKeyCode == ""))
            .Select(x => new { x.DocumentTypeId, x.DocumentType.Name, x.IsMandatory })
            .ToListAsync(ct);

        return new ExpedienteDetailDto(
            header.Id,
            header.ReferenciaNumber,
            header.ClientName,
            header.AduanaName,
            header.OperationTypeName,
            (ReferenciaStatusEnum)header.StatusId,
            header.StatusName,
            header.ArrivalDate,
            header.Notes,
            header.CreatedAt,
            documents.Select(d => new ExpedienteDocumentDto(
                d.Id, (DocumentTypeEnum)d.DocumentTypeId, d.TypeName, d.FileName, d.SizeInBytes,
                Enum.Parse<DocumentStatusEnum>(d.Status, ignoreCase: true), d.UploadedByName, d.UploadedAt)).ToList(),
            // Un tipo puede venir dos veces (obligatorio y opcional): gana obligatorio.
            required
                .GroupBy(r => (r.DocumentTypeId, r.Name))
                .Select(g => new RequiredDocumentDto((DocumentTypeEnum)g.Key.DocumentTypeId, g.Key.Name, g.Any(r => r.IsMandatory)))
                .OrderByDescending(r => r.IsMandatory)
                .ThenBy(r => r.Type)
                .ToList());
    }

    // ILIKE trata % y _ como comodines; el texto del usuario se busca tal cual.
    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
