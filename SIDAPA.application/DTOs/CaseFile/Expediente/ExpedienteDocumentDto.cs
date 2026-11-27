using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.CaseFile.Expediente;

public record ExpedienteDocumentDto(
    int Id,
    DocumentTypeEnum Type,
    string TypeName,
    string FileName,
    long SizeInBytes,
    DocumentStatusEnum Status,
    string UploadedByName,
    DateTime UploadedAt);
