using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.Entities;
using StudyTime.Infrastructure.Common.Transactions;
using StudyTime.Infrastructure.Persistence;

namespace Infrastructure.Tests.Common.Transactions;

public sealed class EfUnitOfWorkTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=studytime;Username=studytime;Password=studytime";

    [Fact]
    public async Task ShouldCommitChangesThroughExistingDbContext()
    {
        await using var context = CreateContext();
        await EnsureDatabaseAsync(context);
        var studyAreaId = Guid.NewGuid();

        try
        {
            var unitOfWork = new EfUnitOfWork(context);

            await using (var transaction = await unitOfWork.BeginTransactionAsync())
            {
                context.StudyAreas.Add(CreateStudyArea(studyAreaId, "Committed"));
                await unitOfWork.SaveChangesAsync();

                Assert.NotNull(context.Database.CurrentTransaction);

                await transaction.CommitAsync();
            }

            Assert.Null(context.Database.CurrentTransaction);
            context.ChangeTracker.Clear();

            var studyArea = await context.StudyAreas.SingleAsync(x => x.Id == studyAreaId);
            Assert.Equal("Committed", studyArea.Name);
        }
        finally
        {
            await DeleteStudyAreaAsync(context, studyAreaId);
        }
    }

    [Fact]
    public async Task ShouldRollbackChangesExplicitly()
    {
        await using var context = CreateContext();
        await EnsureDatabaseAsync(context);
        var studyAreaId = Guid.NewGuid();

        try
        {
            var unitOfWork = new EfUnitOfWork(context);

            await using (var transaction = await unitOfWork.BeginTransactionAsync())
            {
                context.StudyAreas.Add(CreateStudyArea(studyAreaId, "Rolled Back"));
                await unitOfWork.SaveChangesAsync();

                Assert.NotNull(context.Database.CurrentTransaction);

                await transaction.RollbackAsync();
            }

            Assert.Null(context.Database.CurrentTransaction);
            context.ChangeTracker.Clear();

            Assert.False(await context.StudyAreas.AnyAsync(x => x.Id == studyAreaId));
        }
        finally
        {
            await DeleteStudyAreaAsync(context, studyAreaId);
        }
    }

    [Fact]
    public async Task ShouldRollbackAllChangesWhenFailureOccurs()
    {
        await using var context = CreateContext();
        await EnsureDatabaseAsync(context);
        var studyAreaId = Guid.NewGuid();

        try
        {
            var unitOfWork = new EfUnitOfWork(context);

            await using (var transaction = await unitOfWork.BeginTransactionAsync())
            {
                var firstStudyArea = CreateStudyArea(studyAreaId, "First Operation");
                context.StudyAreas.Add(firstStudyArea);
                await unitOfWork.SaveChangesAsync();

                context.Entry(firstStudyArea).State = EntityState.Detached;

                var duplicateStudyArea = CreateStudyArea(studyAreaId, "Failing Operation");
                context.StudyAreas.Add(duplicateStudyArea);

                await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());

                Assert.NotNull(context.Database.CurrentTransaction);

                await transaction.RollbackAsync();
            }

            Assert.Null(context.Database.CurrentTransaction);
            context.ChangeTracker.Clear();

            Assert.False(await context.StudyAreas.AnyAsync(x => x.Id == studyAreaId));
        }
        finally
        {
            await DeleteStudyAreaAsync(context, studyAreaId);
        }
    }

    [Fact]
    public async Task ShouldRollbackWhenTransactionIsDisposedWithoutCommit()
    {
        await using var context = CreateContext();
        await EnsureDatabaseAsync(context);
        var studyAreaId = Guid.NewGuid();

        try
        {
            var unitOfWork = new EfUnitOfWork(context);

            await using (var transaction = await unitOfWork.BeginTransactionAsync())
            {
                context.StudyAreas.Add(CreateStudyArea(studyAreaId, "Uncommitted"));
                await unitOfWork.SaveChangesAsync();

                Assert.NotNull(context.Database.CurrentTransaction);
            }

            Assert.Null(context.Database.CurrentTransaction);
            context.ChangeTracker.Clear();

            Assert.False(await context.StudyAreas.AnyAsync(x => x.Id == studyAreaId));
        }
        finally
        {
            await DeleteStudyAreaAsync(context, studyAreaId);
        }
    }

    [Fact]
    public async Task ShouldRejectOperationsAfterCommit()
    {
        await using var context = CreateContext();
        await EnsureDatabaseAsync(context);
        var studyAreaId = Guid.NewGuid();

        try
        {
            var unitOfWork = new EfUnitOfWork(context);

            await using var transaction = await unitOfWork.BeginTransactionAsync();

            context.StudyAreas.Add(CreateStudyArea(studyAreaId, "Completed"));
            await unitOfWork.SaveChangesAsync();

            await transaction.CommitAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.RollbackAsync());
        }
        finally
        {
            await DeleteStudyAreaAsync(context, studyAreaId);
        }
    }

    [Fact]
    public async Task ShouldUseTheSameDbContextForUnitOfWorkAndTransaction()
    {
        await using var context = CreateContext();
        await EnsureDatabaseAsync(context);

        var unitOfWork = new EfUnitOfWork(context);

        Assert.Null(context.Database.CurrentTransaction);

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        Assert.NotNull(context.Database.CurrentTransaction);

        await transaction.RollbackAsync();

        Assert.Null(context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task ShouldSupportCancellationToken()
    {
        await using var context = CreateContext();
        await EnsureDatabaseAsync(context);
        var studyAreaId = Guid.NewGuid();

        try
        {
            var unitOfWork = new EfUnitOfWork(context);
            using var cancellationTokenSource = new CancellationTokenSource();

            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationTokenSource.Token);

            context.StudyAreas.Add(CreateStudyArea(studyAreaId, "Async"));
            var affectedRows = await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token);

            Assert.Equal(1, affectedRows);

            await transaction.CommitAsync(cancellationTokenSource.Token);
        }
        finally
        {
            await DeleteStudyAreaAsync(context, studyAreaId);
        }
    }

    private static StudyTimeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StudyTimeDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new StudyTimeDbContext(options);
    }

    private static async Task EnsureDatabaseAsync(StudyTimeDbContext context)
    {
        await context.Database.MigrateAsync();
    }

    private static async Task DeleteStudyAreaAsync(StudyTimeDbContext context, Guid studyAreaId)
    {
        context.ChangeTracker.Clear();

        await context.StudyAreas
            .Where(x => x.Id == studyAreaId)
            .ExecuteDeleteAsync();
    }

    private static StudyArea CreateStudyArea(Guid id, string name)
        => StudyArea.Create(id, name, 60);
}