using SIDAPA.application.Interface.Core.Persistence;

namespace SIDAPA.application.test.Fakes;

/// <summary>
/// Registra si la transacción se confirmó o se deshizo (se liberó sin Commit). No deshace el estado de los
/// demás fakes: que el rollback real deshaga todo lo prueban las pruebas de infraestructura con PostgreSQL.
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Begun { get; private set; }
    public int Committed { get; private set; }
    public int RolledBack { get; private set; }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        Begun++;
        return Task.FromResult<IUnitOfWorkTransaction>(new Transaction(this));
    }

    private sealed class Transaction(FakeUnitOfWork owner) : IUnitOfWorkTransaction
    {
        private bool _committed;

        public Task CommitAsync(CancellationToken ct = default)
        {
            _committed = true;
            owner.Committed++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_committed)
                owner.RolledBack++;
            return ValueTask.CompletedTask;
        }
    }
}
