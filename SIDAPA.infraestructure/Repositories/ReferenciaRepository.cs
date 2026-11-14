using Microsoft.EntityFrameworkCore;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.Repositories;

public class ReferenciaRepository(SidapaDbContext context) : IReferenciaRepository
{
    public Task<ReferenciaEntity?> GetByIdAsync(int referenciaId, CancellationToken ct = default) =>
        context.Referencias
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == referenciaId, ct);

    public async Task<IReadOnlyList<short>> GetMandatoryDocumentTypeIdsAsync(short operationTypeCode, CancellationToken ct = default) =>
        await context.RequiredDocuments
            .AsNoTracking()
            .Where(x => x.OperationTypeCode == operationTypeCode
                        && x.IsMandatory
                        && (x.PedimentoKeyCode == null || x.PedimentoKeyCode == ""))   // vacío = aplica a todas
            .Select(x => x.DocumentTypeId)
            .Distinct()
            .ToListAsync(ct);

    public async Task<bool> ChangeStatusAsync(int referenciaId, ReferenciaStatusEnum from, ReferenciaStatusEnum to,
        int userId, string? comment, DateTime changedAt, CancellationToken ct = default)
    {
        var referencia = await context.Referencias
            .FirstOrDefaultAsync(x => x.Id == referenciaId && x.StatusId == (short)from, ct);
        if (referencia is null)
            return false;

        referencia.StatusId = (short)to;
        context.StatusHistories.Add(new StatusHistoryEntity
        {
            ReferenciaId = referenciaId,
            StatusId = (short)to,
            UserId = userId,
            ChangedAt = changedAt,
            Comment = comment
        });

        await context.SaveChangesAsync(ct);   // un solo SaveChanges = una sola transacción
        return true;
    }
}