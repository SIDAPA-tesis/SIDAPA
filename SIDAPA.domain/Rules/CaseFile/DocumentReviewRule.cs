using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CaseFile;

namespace SIDAPA.domain.Rules.CaseFile;

/// <summary>
/// HU-03: los documentos se marcan mientras la referencia está Recibida o En revisión;
/// el primer documento marcado la pasa a En revisión.
/// </summary>
public static class DocumentReviewRule
{
    // RECIBIDO es el estado con el que llega el archivo, no una marca de revisión.
    private static readonly DocumentStatusEnum[] Marks =
        [DocumentStatusEnum.CORRECTO, DocumentStatusEnum.FALTANTE, DocumentStatusEnum.ERROR];

    public static bool CanReview(ReferenciaStatusEnum status) =>
        status is ReferenciaStatusEnum.RECIBIDA or ReferenciaStatusEnum.EN_REVISION;

    public static void EnsureCanReview(ReferenciaStatusEnum status)
    {
        if (!CanReview(status))
            throw new ExpedienteNotInReviewException();
    }

    public static void EnsureValidMark(DocumentStatusEnum status)
    {
        if (!Marks.Contains(status))
            throw new InvalidDocumentReviewStatusException();
    }

    public static bool StartsReview(ReferenciaStatusEnum status) => status == ReferenciaStatusEnum.RECIBIDA;
}
