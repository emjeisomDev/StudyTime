using Xunit;
using Microsoft.EntityFrameworkCore;
using StudyTime.Application.Abstractions;
using StudyTime.Domain.Entities;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Persistence.Transactions;
using Infrastructure.Tests.Containers;

namespace Infrastructure.Tests.Persistence.Transactions;

public sealed class EfUnitOfWorkTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public EfUnitOfWorkTests(PostgreSqlContainerFixture fixture)
        => _fixture = fixture;

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task SaveChangesAsync_WhenEntityIsAdded_PersistsEntity()
    {
        await using StudyTimeDbContext context =
            TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        await context.Database.EnsureCreatedAsync();

        EfUnitOfWork unitOfWork = new(context);

        StudyArea entity = StudyArea.Create("Matemática", 300);

        context.StudyAreas.Add(entity);

        await unitOfWork.SaveChangesAsync();

        StudyArea? persisted = await context.StudyAreas
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == entity.Id);

        Assert.NotNull(persisted);
        Assert.Equal("Matemática", persisted.Name);
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task BeginTransactionAsync_AndRollbackAsync_DiscardsChanges()
    {
        await using StudyTimeDbContext context =
            TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        await context.Database.EnsureCreatedAsync();

        EfUnitOfWork unitOfWork = new(context);

        await unitOfWork.BeginTransactionAsync();

        StudyArea entity = StudyArea.Create("História", 300);

        context.StudyAreas.Add(entity);

        await unitOfWork.SaveChangesAsync();

        await using ITransactionScope transactionScope = unitOfWork.GetCurrentTransactionScope();

        await transactionScope.RollbackAsync();

        await using StudyTimeDbContext verificationContext = TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        bool exists = await verificationContext.StudyAreas
            .AsNoTracking()
            .AnyAsync(x => x.Name == "História");

        Assert.False(exists);
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task BeginTransactionAsync_AndCommitAsync_PersistsChanges()
    {
        await using StudyTimeDbContext context = TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        await context.Database.EnsureCreatedAsync();

        EfUnitOfWork unitOfWork = new(context);

        await unitOfWork.BeginTransactionAsync();

        StudyArea entity = StudyArea.Create("Biologia", 300);

        context.StudyAreas.Add(entity);

        await unitOfWork.SaveChangesAsync();

        await using ITransactionScope transactionScope = unitOfWork.GetCurrentTransactionScope();

        await transactionScope.CommitAsync();

        await using StudyTimeDbContext verificationContext =
            TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        bool exists = await verificationContext.StudyAreas
            .AsNoTracking()
            .AnyAsync(x => x.Name == "Biologia");

        Assert.True(exists);
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task SaveChangesAsync_WhenCancellationTokenIsCanceled_ThrowsOperationCanceledException()
    {
        await using StudyTimeDbContext context =
            TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        await context.Database.EnsureCreatedAsync();

        EfUnitOfWork unitOfWork = new(context);

        StudyArea entity = StudyArea.Create("Física", 300);

        context.StudyAreas.Add(entity);

        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => unitOfWork.SaveChangesAsync(
                cancellationTokenSource.Token));
    }
}