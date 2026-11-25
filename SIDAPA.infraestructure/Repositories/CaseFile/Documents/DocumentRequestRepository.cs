using Microsoft.EntityFrameworkCore;
using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.application.Interface.CaseFile.Documents;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.Repositories.CaseFile.Documents;

/// <summary>ope_solicitud_documento (HU-04 / HU-05, SCRUM-64).</summary>
public class DocumentRequestRepository(SidapaDbContext context) : IDocumentRequestRepository
{
    public async Task CreateAsync(DocumentRequestEntity request, CancellationToken ct = default)
    {
        context.DocumentRequests.Add(request);
        await context.SaveChangesAsync(ct);
    }

    public Task<DocumentRequestDetailDto?> GetForClientAsync(int requestId, int clientId, CancellationToken ct = default) =>
        context.DocumentRequests
            .AsNoTracking()
            .Where(x => x.Id == requestId && x.Referencia.ClientId == clientId)
            .Select(x => new DocumentRequestDetailDto(
                x.Id,
                x.ReferenciaId,
                x.Referencia.ReferenciaNumber,
                (ReferenciaStatusEnum)x.Referencia.StatusId,
                (DocumentTypeEnum)x.DocumentTypeId,
                x.DocumentType.Name,
                x.Note,
                x.RequestedByUserId,
                x.RequestedAt,
                x.AttendedAt != null))
            .FirstOrDefaultAsync(ct);

    // Solo las de referencias que siguen esperando documentos: son las únicas que el cliente puede subir.
    public async Task<IReadOnlyList<PendingDocumentRequestDto>> GetOpenByClientAsync(int clientId, CancellationToken ct = default) =>
        await context.DocumentRequests
            .AsNoTracking()
            .Where(x => x.AttendedAt == null
                        && x.Referencia.ClientId == clientId
                        && x.Referencia.StatusId == (short)ReferenciaStatusEnum.EN_ESPERA_DE_DOCUMENTOS)
            .OrderBy(x => x.RequestedAt)
            .ThenBy(x => x.Id)
            .Select(x => new PendingDocumentRequestDto(
                x.Id,
                x.ReferenciaId,
                x.Referencia.ReferenciaNumber,
                (DocumentTypeEnum)x.DocumentTypeId,
                x.DocumentType.Name,
                x.Note,
                x.RequestedAt))
            .ToListAsync(ct);

    public async Task<bool> MarkAttendedAsync(int requestId, int documentId, DateTime attendedAt, CancellationToken ct = default)
    {
        // La condición "sigue abierta" va en el mismo UPDATE: de dos envíos simultáneos solo uno la cierra.
        var updated = await context.DocumentRequests
            .Where(x => x.Id == requestId && x.AttendedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ResponseDocumentId, documentId)
                .SetProperty(x => x.AttendedAt, attendedAt), ct);
        return updated == 1;
    }

    public Task<int> CountOpenAsync(int referenciaId, CancellationToken ct = default) =>
        context.DocumentRequests.CountAsync(x => x.ReferenciaId == referenciaId && x.AttendedAt == null, ct);
}
