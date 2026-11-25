namespace SIDAPA.presentation.Models.CaseFile.Documents;

/// <summary>Un documento que el Capturador le pidió al cliente (renglón de la tarjeta "Documentos que te pedimos").</summary>
/// <param name="Solicitada">Ya en la hora local del panel.</param>
public record SolicitudPendienteViewModel(int RequestId, string Documento, string? Nota, DateTime Solicitada);
