namespace SIDAPA.application.DTOs.CaseFile.Documents;

/// <param name="Remaining">Solicitudes que siguen abiertas en la referencia.</param>
/// <param name="ReturnedToReview">true si con este documento la referencia regresó a "En revisión".</param>
public record DocumentRequestAttendedDto(
    string ReferenciaNumber,
    string DocumentTypeName,
    int Remaining,
    bool ReturnedToReview);
