using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.Core.Documents;

public record DocumentDto(
    int Id,
    int ReferenciaId,
    DocumentTypeEnum DocumentType,
    string FileName,
    string BucketPath,
    long SizeInBytes,
    DocumentStatusEnum Status,
    int UploadedByUserId,
    DateTime UploadedAt);