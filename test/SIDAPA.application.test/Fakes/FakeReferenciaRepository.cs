using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.test.Fakes;

/// <summary>
/// Tablas ope_referencia + ope_bitacora_estatus en memoria. <see cref="ChangeStatusAsync"/> respeta la
/// condición "solo si sigue en <c>from</c>", igual que el repositorio real.
/// </summary>
public sealed class FakeReferenciaRepository : IReferenciaRepository
{
    private readonly Dictionary<int, ReferenciaEntity> _store = [];
    private long _sequence;

    public List<ReferenciaEntity> Added { get; } = [];
    public List<int> Deleted { get; } = [];
    public int AddedByUserId { get; private set; }
    public List<(ReferenciaStatusEnum From, ReferenciaStatusEnum To, int UserId)> Changes { get; } = [];

    /// <summary>Simula que otro usuario cambió el estatus entre la lectura y la escritura.</summary>
    public bool SimulateConcurrentChange { get; set; }

    /// <summary>Documentos obligatorios que regresa para cualquier tipo de operación.</summary>
    public IReadOnlyList<short> MandatoryDocumentTypeIds { get; set; } = [];

    public ReferenciaEntity Seed(ReferenciaEntity referencia)
    {
        _store[referencia.Id] = referencia;
        return referencia;
    }

    public Task<ReferenciaEntity?> GetByIdAsync(int referenciaId, CancellationToken ct = default) =>
        Task.FromResult(_store.GetValueOrDefault(referenciaId));

    public Task<IReadOnlyList<short>> GetMandatoryDocumentTypeIdsAsync(short operationTypeCode, CancellationToken ct = default) =>
        Task.FromResult(MandatoryDocumentTypeIds);

    public Task<bool> ChangeStatusAsync(int referenciaId, ReferenciaStatusEnum from, ReferenciaStatusEnum to,
        int userId, string? comment, DateTime changedAt, CancellationToken ct = default)
    {
        if (SimulateConcurrentChange || !_store.TryGetValue(referenciaId, out var referencia) || referencia.StatusId != (short)from)
            return Task.FromResult(false);

        referencia.StatusId = (short)to;
        Changes.Add((from, to, userId));
        return Task.FromResult(true);
    }

    /// <summary>Referencias bloqueadas con <see cref="LockStatusAsync"/> (el candado real lo prueba infraestructura).</summary>
    public List<int> Locked { get; } = [];

    public Task<ReferenciaStatusEnum?> LockStatusAsync(int referenciaId, CancellationToken ct = default)
    {
        Locked.Add(referenciaId);
        return Task.FromResult(_store.TryGetValue(referenciaId, out var referencia)
            ? (ReferenciaStatusEnum?)referencia.StatusId
            : null);
    }

    public Task<bool> AssignCapturadorAsync(int referenciaId, int userId, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(referenciaId, out var referencia) || referencia.CapturadorId is not null)
            return Task.FromResult(false);

        referencia.CapturadorId = userId;
        return Task.FromResult(true);
    }

    public Task<long> NextNumberAsync(CancellationToken ct = default) => Task.FromResult(++_sequence);

    public Task AddAsync(ReferenciaEntity referencia, int userId, CancellationToken ct = default)
    {
        referencia.Id = _store.Count + 1;
        _store[referencia.Id] = referencia;
        Added.Add(referencia);
        AddedByUserId = userId;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int referenciaId, CancellationToken ct = default)
    {
        _store.Remove(referenciaId);
        Deleted.Add(referenciaId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ClientReferenciaDto>> GetByClientAsync(int clientId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ClientReferenciaDto>>([]);
}
