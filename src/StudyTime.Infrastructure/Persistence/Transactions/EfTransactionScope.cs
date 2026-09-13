using Microsoft.EntityFrameworkCore.Storage;
using StudyTime.Application.Abstractions;

namespace StudyTime.Infrastructure.Persistence.Transactions;

public sealed class EfTransactionScope : ITransactionScope
{
    private readonly StudyTimeDbContext _dbContext;

    private bool _completed;
    private bool _disposed;

    public EfTransactionScope(StudyTimeDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task CommitAsync(CancellationToken token = default)
    {
        EnsureNotDisposed();
        EnsureNotCompleted();

        IDbContextTransaction transaction = GetCurrentTransaction();
        await transaction.CommitAsync(token);
        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken token = default)
    {
        EnsureNotDisposed();
        EnsureNotCompleted();

        IDbContextTransaction transaction = GetCurrentTransaction();
        await transaction.RollbackAsync(token);
        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (!_completed)
            {
                IDbContextTransaction? transaction = _dbContext.Database.CurrentTransaction;

                if (transaction is not null)
                {
                    await transaction.RollbackAsync();
                }
            }
        }
        finally
        {
            _disposed = true;
        }
    }

    private IDbContextTransaction GetCurrentTransaction()
    {
        return _dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "No active transaction has been started for the current context.");
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void EnsureNotCompleted()
    {
        if (_completed)
        {
            throw new InvalidOperationException("The transaction has already been completed and cannot be reused.");
        }
    }
}