using SIDAPA.application.DTOs.Core.Referencias;

namespace SIDAPA.application.Interface.Core;

/// <summary>Catálogos del Anexo 22 que se muestran en pantallas de captura.</summary>
public interface ICatalogRepository
{
    Task<IReadOnlyList<AduanaOptionDto>> GetActiveAduanasAsync(CancellationToken ct = default);
    Task<bool> IsActiveAduanaAsync(string aduanaCode, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentTypeOptionDto>> GetDocumentTypesAsync(CancellationToken ct = default);
}
