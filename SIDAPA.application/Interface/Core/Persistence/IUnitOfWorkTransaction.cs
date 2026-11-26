namespace SIDAPA.application.Interface.Core.Persistence;

/// <summary>Transacción abierta por <see cref="IUnitOfWork"/>. Si se libera sin <see cref="CommitAsync"/>, se deshace.</summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}
