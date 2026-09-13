using Microsoft.EntityFrameworkCore.Storage;
using StudyTime.Application.Abstractions;

namespace StudyTime.Infrastructure.Persistence.Transactions;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly StudyTimeDbContext _dbContext;

    private IDbContextTransaction? _currentTransaction;

    public EfUnitOfWork(StudyTimeDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is not null)
        {
            throw new InvalidOperationException(
                "An active transaction already exists for this Unit of Work..");
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(ct);
    }

    public ITransactionScope GetCurrentTransactionScope()
    {
        return _currentTransaction is null
            ? throw new InvalidOperationException(
                "No active transaction has been initiated.")
            : new EfTransactionScope(_currentTransaction);
    }
}