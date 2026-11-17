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
}