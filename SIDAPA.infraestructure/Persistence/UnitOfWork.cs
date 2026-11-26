using Microsoft.EntityFrameworkCore.Storage;
using SIDAPA.application.Interface.Core.Persistence;

namespace SIDAPA.infraestructure.Persistence;

/// <summary>
/// Transacción sobre el <see cref="SidapaDbContext"/> de la petición. Como el contexto es scoped, todos los
/// repositorios que se usen mientras está abierta escriben dentro de ella.
/// </summary>
public class UnitOfWork(SidapaDbContext context) : IUnitOfWork
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        new Transaction(await context.Database.BeginTransactionAsync(ct));

    /// <summary>Si se libera sin Commit, EF hace rollback.</summary>
    private sealed class Transaction(IDbContextTransaction inner) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => inner.CommitAsync(ct);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
