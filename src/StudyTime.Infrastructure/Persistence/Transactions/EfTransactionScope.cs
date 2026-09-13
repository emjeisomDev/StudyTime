using Microsoft.EntityFrameworkCore.Storage;
using StudyTime.Application.Abstractions;

namespace StudyTime.Infrastructure.Persistence.Transactions;

public sealed class EfTransactionScope : ITransactionScope
{
    private readonly IDbContextTransaction _transaction;

    private bool _completed;
    private bool _disposed;

    public EfTransactionScope(IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        _transaction = transaction;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();
        EnsureNotCompleted();

        await _transaction.CommitAsync(ct);

        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();
        EnsureNotCompleted();

        await _transaction.RollbackAsync(ct);

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
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            await _transaction.DisposeAsync();
            _disposed = true;
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void EnsureNotCompleted()
    {
        if (_completed)
        {
            throw new InvalidOperationException("A transação já foi finalizada e não pode ser reutilizada.");
        }
    }
}