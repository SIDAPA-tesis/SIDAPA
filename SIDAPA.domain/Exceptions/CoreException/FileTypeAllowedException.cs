namespace SIDAPA.domain.Exceptions.CoreException;

public class FileTypeNotAllowedException(string fileName) : DomainException(
    $"El archivo \"{fileName}\" no es de un tipo permitido. Solo se aceptan PDF, XML o Excel (.xls, .xlsx.");
