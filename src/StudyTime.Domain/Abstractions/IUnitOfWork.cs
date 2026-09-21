namespace StudyTime.Domain.Abstractions;

public interface IUnitOfWork
{
    public Task CommitAsync(CancellationToken token);
    public Task RollbackAsync(CancellationToken token); 
}
