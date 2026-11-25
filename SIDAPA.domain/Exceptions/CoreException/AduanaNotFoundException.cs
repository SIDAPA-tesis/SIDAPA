namespace SIDAPA.domain.Exceptions.CoreException;

public class AduanaNotFoundException(string? aduanaCode) : DomainException(
    $"La aduana \"{aduanaCode}\" no existe o ya no está vigente en el Apéndice 1.");
