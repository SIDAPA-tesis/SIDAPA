// ReferenciaNotFoundException.cs
namespace SIDAPA.domain.Exceptions.CoreException;

public class ReferenciaNotFoundException(int referenciaId) : DomainException($"No existe la referencia {referenciaId}.");