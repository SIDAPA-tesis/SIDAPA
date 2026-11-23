using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.Interface.Core;

public interface IReferenciaRepository
{
    Task<ReferenciaEntity?> GetByIdAsync(int referenciaId, CancellationToken ct = default);

    /// <summary>Documentos obligatorios del tipo de operación (los que aplican a cualquier clave de pedimento).</summary>
    Task<IReadOnlyList<short>> GetMandatoryDocumentTypeIdsAsync(short operationTypeCode, CancellationToken ct = default);

    /// <summary>
    /// Cambia el estatus solo si la referencia sigue en <paramref name="from"/> y deja el registro en
    /// ope_bitacora_estatus, todo en una sola transacción. Regresa false si ya estaba en otro estatus.
    /// </summary>
    Task<bool> ChangeStatusAsync(int referenciaId, ReferenciaStatusEnum from, ReferenciaStatusEnum to,
        int userId, string? comment, DateTime changedAt, CancellationToken ct = default);

    /// <summary>Siguiente valor de la secuencia del número de referencia (sin huecos garantizados, pero sin duplicados).</summary>
    Task<long> NextNumberAsync(CancellationToken ct = default);

    /// <summary>Inserta la referencia y su primer registro en la bitácora de estatus, en una sola transacción.</summary>
    Task AddAsync(ReferenciaEntity referencia, int userId, CancellationToken ct = default);

    /// <summary>Borra la referencia con su bitácora y sus documentos (compensación cuando falla el aviso).</summary>
    Task DeleteAsync(int referenciaId, CancellationToken ct = default);

    Task<IReadOnlyList<ClientReferenciaDto>> GetByClientAsync(int clientId, CancellationToken ct = default);
}
