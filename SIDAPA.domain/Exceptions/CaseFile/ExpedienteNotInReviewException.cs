using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.domain.Exceptions.CaseFile;

public class ExpedienteNotInReviewException()
    : DomainException("Esta referencia ya no está en revisión: no se pueden cambiar sus documentos.");
