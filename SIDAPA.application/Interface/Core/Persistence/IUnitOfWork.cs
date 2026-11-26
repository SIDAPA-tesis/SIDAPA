namespace SIDAPA.application.Interface.Core.Persistence;

/// <summary>
/// Agrupa en una sola transacción de BD lo que hacen varios repositorios de la misma petición
/// (comparten el DbContext). Lo que no es de la BD (bucket, correo) va fuera de la transacción.
/// </summary>
public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
