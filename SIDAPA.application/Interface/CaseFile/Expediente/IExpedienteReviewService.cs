using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.application.DTOs.Core.Referencias;

namespace SIDAPA.application.Interface.CaseFile.Expediente;

/// <summary>HU-03: el personal consulta el expediente y el Capturador marca cada documento.</summary>
public interface IExpedienteReviewService
{
    Task<IReadOnlyList<ExpedienteSummaryDto>> SearchAsync(ExpedienteFilterDto filter, CancellationToken ct = default);

    Task<ExpedienteReviewDto?> GetAsync(int referenciaId, CancellationToken ct = default);

    Task<DocumentPreviewDto?> GetPreviewAsync(int documentId, CancellationToken ct = default);

    /// <summary>Solo PDF y XML, que el navegador muestra sin descargar; para Excel usa <see cref="GetPreviewAsync"/>.</summary>
    Task<DocumentFileDto?> OpenFileAsync(int documentId, CancellationToken ct = default);

    /// <summary>
    /// Marca el documento. El primero que se marca pasa la referencia de Recibida a En revisión; si con esto quedan
    /// correctos todos los obligatorios, pasa a Expediente completo y se avisa al cliente.
    /// </summary>
    Task<ExpedienteStatusDto> MarkAsync(ReviewDocumentDto review, CancellationToken ct = default);
}
