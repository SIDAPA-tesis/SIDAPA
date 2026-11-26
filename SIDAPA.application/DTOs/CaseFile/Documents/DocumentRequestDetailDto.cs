using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.CaseFile.Documents;

/// <summary>Una solicitud de documento con lo necesario para atenderla.</summary>
public record DocumentRequestDetailDto(
    int RequestId,
    int ReferenciaId,
    string ReferenciaNumber,
    ReferenciaStatusEnum ReferenciaStatus,
    DocumentTypeEnum DocumentType,
    string DocumentTypeName,
    string? Note,
    int RequestedByUserId,
    DateTime RequestedAt,
    bool IsAttended);
