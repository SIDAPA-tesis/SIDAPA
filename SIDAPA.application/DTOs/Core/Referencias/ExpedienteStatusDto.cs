using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.Core.Referencias;

/// <summary>Cómo va el expediente de una referencia contra su lista de documentos obligatorios.</summary>
public record ExpedienteStatusDto(
    int ReferenciaId,
    ReferenciaStatusEnum Status,
    IReadOnlyList<DocumentTypeEnum> MandatoryDocuments,
    IReadOnlyList<DocumentTypeEnum> MissingDocuments,
    bool IsComplete,
    bool StatusChanged);