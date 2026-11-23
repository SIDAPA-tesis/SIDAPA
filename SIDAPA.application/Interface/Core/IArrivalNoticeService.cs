using SIDAPA.application.DTOs.Core.Referencias;

namespace SIDAPA.application.Interface.Core;

/// <summary>Aviso de arribo del Cliente (HU-02 / SCRUM-46).</summary>
public interface IArrivalNoticeService
{
    /// <summary>
    /// Valida todo (datos y archivos) antes de crear nada; luego crea la referencia en estatus RECIBIDA con su
    /// número único y sube los documentos. Si algo falla a medio camino, deshace lo que alcanzó a crear.
    /// </summary>
    Task<ArrivalNoticeCreatedDto> CreateAsync(CreateArrivalNoticeDto notice, CancellationToken ct = default);

    Task<IReadOnlyList<ClientReferenciaDto>> GetClientPanelAsync(int clientId, CancellationToken ct = default);

    /// <summary>Aduanas vigentes y tipos de documento para el formulario.</summary>
    Task<ArrivalNoticeCatalogsDto> GetCatalogsAsync(CancellationToken ct = default);
}
