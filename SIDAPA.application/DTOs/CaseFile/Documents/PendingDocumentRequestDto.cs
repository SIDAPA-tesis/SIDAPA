using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.CaseFile.Documents;

/// <summary>Documento que el Capturador le pidió al cliente y que sigue sin subirse (HU-05).</summary>
public record PendingDocumentRequestDto(
    int RequestId,
    int ReferenciaId,
    string ReferenciaNumber,
    DocumentTypeEnum DocumentType,
    string DocumentTypeName,
    string? Note,
    DateTime RequestedAt);
