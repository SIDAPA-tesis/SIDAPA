using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.domain.Exceptions.CaseFile;

/// <summary>La referencia ya no está en "En espera de documentos": ya no se reciben documentos solicitados.</summary>
public class ReferenciaNotAwaitingDocumentsException()
    : DomainException("Esta referencia ya no está esperando documentos.");
