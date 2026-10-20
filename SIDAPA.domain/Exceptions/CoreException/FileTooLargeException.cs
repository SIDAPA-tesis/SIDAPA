namespace SIDAPA.domain.Exceptions.CoreException;

public class FileTooLargeException(string fileName, long sizeInBytes, long maxSizeInBytes) : DomainException(
    $"El archivo \"{fileName}\" pesa {sizeInBytes / 1048576d:0.#} MB; el maximo es {maxSizeInBytes / 1048576} MB.");
