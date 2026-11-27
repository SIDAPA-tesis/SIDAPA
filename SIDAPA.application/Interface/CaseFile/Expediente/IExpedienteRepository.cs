using SIDAPA.application.DTOs.CaseFile.Expediente;

namespace SIDAPA.application.Interface.CaseFile.Expediente;

public interface IExpedienteRepository
{
    /// <summary>Más recientes primero; la búsqueda es por número de referencia o razón social.</summary>
    Task<ExpedientePageDto> SearchAsync(ExpedienteFilterDto filter, int pageSize, CancellationToken ct = default);

    Task<ExpedienteDetailDto?> GetDetailAsync(int referenciaId, CancellationToken ct = default);
}
