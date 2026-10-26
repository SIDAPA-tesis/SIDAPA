namespace SIDAPA.domain.Exceptions.CoreException;

public class NotificationMessageTooLongException(int maxLenght) : DomainException(
    $"El aviso es demasiado largo: maximo {maxLenght} caracteres."
);