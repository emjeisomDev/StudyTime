using Xunit;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Tests.Containers;
using StudyTime.Application.Abstractions;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Persistence.Transactions;
using StudyTime.Domain.Entities;

namespace Infrastructure.Tests.Persistence.Transactions;

public sealed class EfTransactionScopeTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public EfTransactionScopeTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task DisposeAsync_WhenTransactionIsNotCompleted_RollsBackChanges()
    {
        await using StudyTimeDbContext context =
            TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        await context.Database.EnsureCreatedAsync();

        EfUnitOfWork unitOfWork = new(context);

        await unitOfWork.BeginTransactionAsync();

        StudyArea entity = StudyArea.Create("Química", 300);
        context.StudyAreas.Add(entity);

        await unitOfWork.SaveChangesAsync();

        ITransactionScope transactionScope =
            unitOfWork.GetCurrentTransactionScope();

        await transactionScope.DisposeAsync();

        await using StudyTimeDbContext verificationContext =
            TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        bool exists = await verificationContext.StudyAreas
            .AsNoTracking()
            .AnyAsync(x => x.Name == "Química");

        Assert.False(exists);
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task CommitAsync_WhenCalledTwice_ThrowsInvalidOperationException()
    {
        await using StudyTimeDbContext context =
            TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        await context.Database.EnsureCreatedAsync();

        EfUnitOfWork unitOfWork = new(context);

        await unitOfWork.BeginTransactionAsync();

        ITransactionScope transactionScope =
            unitOfWork.GetCurrentTransactionScope();

        await transactionScope.CommitAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => transactionScope.CommitAsync());

        await transactionScope.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task RollbackAsync_WhenCalledTwice_ThrowsInvalidOperationException()
    {
        await using StudyTimeDbContext context =
            TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        await context.Database.EnsureCreatedAsync();

        EfUnitOfWork unitOfWork = new(context);

        await unitOfWork.BeginTransactionAsync();

        ITransactionScope transactionScope =
            unitOfWork.GetCurrentTransactionScope();

        await transactionScope.RollbackAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => transactionScope.RollbackAsync());

        await transactionScope.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task DisposeAsync_WhenCalledTwice_DoesNotThrow()
    {
        await using StudyTimeDbContext context =
            TestStudyTimeDbContextFactory.Create(_fixture.ConnectionString);

        await context.Database.EnsureCreatedAsync();

        EfUnitOfWork unitOfWork = new(context);

        await unitOfWork.BeginTransactionAsync();

        ITransactionScope transactionScope =
            unitOfWork.GetCurrentTransactionScope();

        await transactionScope.DisposeAsync();
        await transactionScope.DisposeAsync();
    }

    private sealed class StudyAreaEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;
    }
}