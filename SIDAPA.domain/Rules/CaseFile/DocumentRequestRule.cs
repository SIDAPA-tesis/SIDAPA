using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CaseFile;

namespace SIDAPA.domain.Rules.CaseFile;

/// <summary>
/// Solicitudes de documentos faltantes (HU-04 / HU-05, ope_solicitud_documento).
/// La referencia está en "En espera de documentos" PORQUE tiene solicitudes abiertas: regresa a
/// "En revisión" solo cuando el cliente atendió la última.
/// </summary>
public static class DocumentRequestRule
{
    /// <summary>El cliente solo puede subir el documento de una solicitud abierta de una referencia que lo espera.</summary>
    public static void EnsureCanAttend(ReferenciaStatusEnum referenciaStatus, bool alreadyAttended)
    {
        if (alreadyAttended)
            throw new DocumentRequestAlreadyAttendedException();

        if (referenciaStatus != ReferenciaStatusEnum.EN_ESPERA_DE_DOCUMENTOS)
            throw new ReferenciaNotAwaitingDocumentsException();
    }

    /// <summary>true cuando ya no queda ninguna solicitud abierta en la referencia.</summary>
    public static bool ShouldReturnToReview(int openRequests)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(openRequests);
        return openRequests == 0;
    }
}
