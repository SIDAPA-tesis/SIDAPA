using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.CaseFile.Expediente;

public record ChecklistItemDto(DocumentTypeEnum Type, string Name, bool IsMandatory, DocumentStatusEnum Status);
