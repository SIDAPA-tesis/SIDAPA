namespace SIDAPA.domain.Exceptions.CoreException;

public class NotesTooLongException(int maxLength) : DomainException(
    $"Las observaciones no pueden pasar de {maxLength} caracteres.");
