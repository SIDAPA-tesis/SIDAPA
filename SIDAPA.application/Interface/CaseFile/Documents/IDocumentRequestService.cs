using SIDAPA.application.DTOs.CaseFile.Documents;

namespace SIDAPA.application.Interface.CaseFile.Documents;

/// <summary>HU-05: el cliente sube el documento que le pidieron y se avisa al Capturador.</summary>
public interface IDocumentRequestService
{
    Task<IReadOnlyList<PendingDocumentRequestDto>> GetPendingAsync(int clientId, CancellationToken ct = default);

    /// <summary>La solicitud lista para subir su documento; lanza si no es del cliente, ya se atendió o la referencia ya no espera documentos.</summary>
    Task<DocumentRequestDetailDto> GetForUploadAsync(int requestId, int clientId, CancellationToken ct = default);

    /// <summary>
    /// Sube el archivo, lo agrega al expediente y cierra la solicitud. Si era la última abierta, la referencia
    /// regresa a "En revisión". Al final avisa a quien pidió el documento.
    /// </summary>
    Task<DocumentRequestAttendedDto> AttendAsync(AttendDocumentRequestDto attend, CancellationToken ct = default);
}
