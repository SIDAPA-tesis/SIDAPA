using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.CaseFile.Expediente;

public record ReviewDocumentDto(int ReferenciaId, int DocumentId, DocumentStatusEnum Status, int UserId);
