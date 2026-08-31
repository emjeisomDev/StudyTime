using Microsoft.EntityFrameworkCore.Storage;
using StudyTime.Application.Common.Transactions;

namespace StudyTime.Infrastructure.Common.Transactions;

public sealed class EfTransaction : ITransaction
{
    private readonly IDbContextTransaction _transaction;
    private bool _completed;
    private bool _disposed;

    public EfTransaction(IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        _transaction = transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        EnsureActive();

        try
        {
            await _transaction.CommitAsync(cancellationToken);
            _completed = true;
        }
        catch
        {
            await RollbackAfterFailureAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        EnsureActive();
        await _transaction.RollbackAsync(cancellationToken);
        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            if (!_completed)
            {
                try
                {
                    await _transaction.RollbackAsync();
                }
                catch
                {
                }
            }
        }
        finally
        {
            await _transaction.DisposeAsync();
            _disposed = true;
            _completed = true;
        }
    }

    private async Task RollbackAfterFailureAsync(CancellationToken cancellationToken)
    {
        if (_completed)
            return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _completed = true;
        }
    }

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_completed)
            throw new InvalidOperationException("The transaction has already been completed.");
    }
}