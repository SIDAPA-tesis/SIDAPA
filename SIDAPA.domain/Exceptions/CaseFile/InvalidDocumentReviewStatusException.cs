using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.domain.Exceptions.CaseFile;

public class InvalidDocumentReviewStatusException()
    : DomainException("Marca el documento como Correcto, Faltante o Con error.");
