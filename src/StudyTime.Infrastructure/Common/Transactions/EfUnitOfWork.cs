using Microsoft.EntityFrameworkCore;
using StudyTime.Application.Common.Transactions;
using StudyTime.Infrastructure.Persistence;

namespace StudyTime.Infrastructure.Common.Transactions;

public sealed class EfUnitOfWork(StudyTimeDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransaction(transaction);
    }
}