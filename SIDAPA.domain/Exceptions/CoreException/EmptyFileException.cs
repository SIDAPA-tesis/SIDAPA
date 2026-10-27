namespace SIDAPA.domain.Exceptions.CoreException;

public class EmptyFileException(string fileName) : DomainException(
    $"El archivo \"{fileName}\" esta vacio.");
