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
        // Dentro de un IUnitOfWork ya hay transacción: se une a ella y el commit lo hace quien la abrió.
        // EF no permite abrir una segunda transacción sobre la misma conexión.
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(ct)
            : null;

        // La condición "sigue en from" va en el mismo UPDATE: si otro usuario la cambió (o tiene el candado
        // de la fila y luego confirma), PostgreSQL vuelve a evaluar el WHERE y no actualiza nada.
        // Leer primero y guardar después pisaba el cambio del otro usuario (lost update).
        var updated = await context.Referencias
            .Where(x => x.Id == referenciaId && x.StatusId == (short)from)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.StatusId, (short)to), ct);
        if (updated == 0)
            return false;

        context.StatusHistories.Add(new StatusHistoryEntity
        {
            ReferenciaId = referenciaId,
            StatusId = (short)to,
            UserId = userId,
            ChangedAt = changedAt,
            Comment = comment
        });

        await context.SaveChangesAsync(ct);
        if (transaction is not null)
            await transaction.CommitAsync(ct);    // estatus + bitácora en una sola transacción
        return true;
    }

    public async Task<ReferenciaStatusEnum?> LockStatusAsync(int referenciaId, CancellationToken ct = default)
    {
        // Sin componer la consulta (nada de First/Single): EF la mandaría como subconsulta.
        var status = await context.Database
            .SqlQueryRaw<short>(
                $"SELECT id_estatus AS \"Value\" FROM \"{SidapaDbContext.Schema}\".ope_referencia WHERE id_referencia = {{0}} FOR UPDATE",
                referenciaId)
            .ToListAsync(ct);

        return status.Count == 0 ? null : (ReferenciaStatusEnum)status[0];
    }

    // ExecuteUpdate corre dentro de la transacción abierta si la hay (IUnitOfWork).
    public async Task<bool> AssignCapturadorAsync(int referenciaId, int userId, CancellationToken ct = default) =>
        await context.Referencias
            .Where(x => x.Id == referenciaId && x.CapturadorId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.CapturadorId, userId), ct) == 1;

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
