using SIDAPA.application.DTOs.Core.Documents;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.Dev;

public class DevDocumentViewModel
{
    public int ReferenciaId { get; set; } = 1;
    public DocumentTypeEnum DocumentType { get; set; } = DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR;
    public IFormFile? File { get; set; }
    public IReadOnlyList<DocumentDto> Documents { get; set; } = [];
}