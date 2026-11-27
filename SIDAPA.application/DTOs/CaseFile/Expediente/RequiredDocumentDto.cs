using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.CaseFile.Expediente;

public record RequiredDocumentDto(DocumentTypeEnum Type, string Name, bool IsMandatory);
