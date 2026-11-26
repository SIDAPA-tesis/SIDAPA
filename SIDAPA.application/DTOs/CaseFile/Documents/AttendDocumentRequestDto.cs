namespace SIDAPA.application.DTOs.CaseFile.Documents;

/// <summary>
/// El cliente sube el documento de una solicitud. El tipo de documento NO viene aquí: sale de la solicitud.
/// <see cref="Content"/> debe permitir Seek (primero se valida y luego se sube).
/// </summary>
public record AttendDocumentRequestDto(
    int RequestId,
    int ClientId,
    int UserId,
    string FileName,
    long SizeInBytes,
    Stream Content);
