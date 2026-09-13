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

    public async Task SaveChangesAsync(CancellationToken token = default)
        => await _dbContext.SaveChangesAsync(token);


    public async Task BeginTransactionAsync(CancellationToken token = default)
    {
        if (_currentTransaction is not null)
        {
            throw new InvalidOperationException("An active transaction already exists for this Unit of Work.");
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(token);
    }

    public ITransactionScope GetCurrentTransactionScope()
    {
        if (_currentTransaction is null)
        {
            throw new InvalidOperationException("No active transaction has been initiated.");
        }

        return new EfTransactionScope(_dbContext);
    }
}