namespace SIDAPA.domain.Exceptions.CoreException;

public class FileContentMismatchException(string fileName) : DomainException(
    $"El archivo \"{fileName}\" no corresponde a su extension. Revisa que el archivo no este daniado o renombrado.");
