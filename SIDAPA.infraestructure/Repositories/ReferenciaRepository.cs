using Microsoft.EntityFrameworkCore;
using SIDAPA.application.DTOs.Core.Referencias;
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

    public Task<long> NextNumberAsync(CancellationToken ct = default) =>
        context.Database
            .SqlQueryRaw<long>(
                $"SELECT nextval('\"{SidapaDbContext.Schema}\".\"{SidapaDbContext.ReferenciaNumberSequence}\"') AS \"Value\"")
            .SingleAsync(ct);

    public async Task AddAsync(ReferenciaEntity referencia, int userId, CancellationToken ct = default)
    {
        referencia.StatusHistories.Add(new StatusHistoryEntity
        {
            StatusId = referencia.StatusId,
            UserId = userId,
            ChangedAt = referencia.CreatedAt,
            Comment = "Aviso de arribo recibido"
        });

        context.Referencias.Add(referencia);
        await context.SaveChangesAsync(ct);   // referencia + bitácora en una sola transacción
    }

    public async Task DeleteAsync(int referenciaId, CancellationToken ct = default)
    {
        // Lo agregado en este contexto ya no debe rastrearse: se borra directo en la BD.
        context.ChangeTracker.Clear();

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.Documents.Where(x => x.ReferenciaId == referenciaId).ExecuteDeleteAsync(ct);
        await context.StatusHistories.Where(x => x.ReferenciaId == referenciaId).ExecuteDeleteAsync(ct);
        await context.Referencias.Where(x => x.Id == referenciaId).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<ClientReferenciaDto>> GetByClientAsync(int clientId, CancellationToken ct = default) =>
        await context.Referencias
            .AsNoTracking()
            .Where(x => x.ClientId == clientId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ClientReferenciaDto(
                x.Id,
                x.ReferenciaNumber,
                x.Aduana.Name,
                x.ArrivalDate,
                (ReferenciaStatusEnum)x.StatusId,
                x.Status.Name,
                x.CreatedAt,
                x.Documents.Count))
            .ToListAsync(ct);
}
