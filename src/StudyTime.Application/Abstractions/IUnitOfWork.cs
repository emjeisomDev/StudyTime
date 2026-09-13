namespace StudyTime.Application.Abstractions;

public interface IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken token = default);
    public Task BeginTransactionAsync(CancellationToken token = default);
}
