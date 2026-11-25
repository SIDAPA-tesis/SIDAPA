using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.domain.Exceptions.CaseFile;

/// <summary>La solicitud no existe o no es de la empresa del usuario (no se distingue a propósito).</summary>
public class DocumentRequestNotFoundException()
    : DomainException("No encontramos el documento solicitado.");
