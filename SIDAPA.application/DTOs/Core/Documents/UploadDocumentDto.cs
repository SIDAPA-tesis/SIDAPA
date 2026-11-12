using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.Core.Documents;

/// <summary>
/// Archivo que se va a guardar en el expediente. <see cref="Content"/> debe permitir Seek
/// (el de <c>IFormFile.OpenReadStream()</c> sí lo permite), porque primero se valida y luego se sube.
/// </summary>
public record UploadDocumentDto(
    int ReferenciaId,
    DocumentTypeEnum DocumentType,
    string FileName,
    long SizeInBytes,
    Stream Content,
    int UploadedByUserId);