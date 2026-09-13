namespace StudyTime.Application.Abstractions;

public interface ITransactionScope
{
    public Task CommitAsync(CancellationToken token = default);
    public Task RollbackAsync(CancellationToken token = default);
}
