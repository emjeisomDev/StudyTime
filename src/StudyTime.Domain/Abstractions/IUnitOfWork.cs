namespace StudyTime.Domain.Abstractions;

public interface IUnitOfWork
{
    public Task CommitAsync(CancellationToken ct);
    public Task RollbackAsync(CancellationToken ct); 
}
