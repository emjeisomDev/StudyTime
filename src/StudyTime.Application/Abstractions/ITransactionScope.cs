namespace StudyTime.Application.Abstractions;

public interface ITransactionScope : IAsyncDisposable
{
    public Task CommitAsync(CancellationToken token = default);
    public Task RollbackAsync(CancellationToken token = default);
}
