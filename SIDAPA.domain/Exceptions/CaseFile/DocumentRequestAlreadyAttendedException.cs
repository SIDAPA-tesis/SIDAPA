using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.domain.Exceptions.CaseFile;

/// <summary>El cliente ya subió el documento de esta solicitud (p. ej. doble envío del formulario).</summary>
public class DocumentRequestAlreadyAttendedException()
    : DomainException("Ya recibimos este documento. No necesitas volver a subirlo.");
