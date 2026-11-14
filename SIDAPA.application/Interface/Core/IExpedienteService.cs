using SIDAPA.application.DTOs.Core.Referencias;

namespace SIDAPA.application.Interface.Core;

/// <summary>Revisa si el expediente de una referencia está completo (SCRUM-56).</summary>
public interface IExpedienteService
{
    /// <summary>Solo consulta qué documentos obligatorios faltan; no cambia nada (sirve para la pantalla de revisión).</summary>
    Task<ExpedienteStatusDto> CheckAsync(int referenciaId, CancellationToken ct = default);

    /// <summary>
    /// Revisa el expediente y, si quedó completo, pasa la referencia de EN_REVISION a EXPEDIENTE_COMPLETO,
    /// guarda la bitácora y avisa al cliente. Se llama cada vez que se marca un documento (SCRUM-55).
    /// </summary>
    Task<ExpedienteStatusDto> EvaluateAsync(int referenciaId, int userId, CancellationToken ct = default);
}