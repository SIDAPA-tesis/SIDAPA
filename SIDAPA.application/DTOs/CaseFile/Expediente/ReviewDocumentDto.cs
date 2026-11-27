using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.CaseFile.Expediente;

/// <param name="Note">Qué falta o qué está mal; se descarta al marcar Correcto.</param>
public record ReviewDocumentDto(int ReferenciaId, int DocumentId, DocumentStatusEnum Status, int UserId, string? Note = null);
