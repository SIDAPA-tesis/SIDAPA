namespace SIDAPA.domain.Exceptions.CoreException;

public class AccountLockedException(TimeSpan remaing) : DomainException(
    $"Tu cuenta esta bloqueada por demasiados intentos fallidos. " +
    $"Intenta de nuevo en {Math.Max(1, (int)Math.Ceiling(remaing.TotalMinutes))} minutos."
);