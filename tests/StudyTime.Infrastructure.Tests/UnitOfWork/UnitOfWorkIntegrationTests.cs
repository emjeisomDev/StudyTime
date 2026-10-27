using Xunit;
using StudyTime.Domain.Entities;
using StudyTime.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Tests.Fixtures;
using InfrastructureUnitOfWork = StudyTime.Infrastructure.Persistence.Repositories.UnitOfWork;

namespace StudyTime.Infrastructure.Tests.UnitOfWork;

[Collection(PostgreSqlCollection.Name)]
public sealed class UnitOfWorkIntegrationTests(PostgreSqlFixture fixture) : PostgreSqlTestBase(fixture)
{
    [Fact]
    public async Task BeginESaveChangesECommit_PersisteAlteracoes()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        InfrastructureUnitOfWork unitOfWork = new(dbContext);
        StudyArea area = new("Persistida após commit", new Minutes(120));

        // Act
        await unitOfWork.BeginTransactionAsync();
        await dbContext.StudyAreas.AddAsync(area);
        await unitOfWork.SaveChangesAsync();
        await unitOfWork.CommitTransactionAsync();

        // Assert
        await using StudyTimeDbContext verificationContext = CreateDbContext();
        StudyArea? persisted = await verificationContext.StudyAreas
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == area.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Persistida após commit", persisted.Name);
    }

    [Fact]
    public async Task ExcecaoAposSaveChangesErollback_NaoPersisteAlteracoes()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        InfrastructureUnitOfWork unitOfWork = new(dbContext);
        StudyArea area = new("Descartada após rollback", new Minutes(90));
        await unitOfWork.BeginTransactionAsync();

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await dbContext.StudyAreas.AddAsync(area);
            await unitOfWork.SaveChangesAsync();
            throw new InvalidOperationException("Falha simulada antes do commit.");
        });
        await unitOfWork.RollbackTransactionAsync();

        // Assert
        Assert.Equal("Falha simulada antes do commit.", exception.Message);
        await using StudyTimeDbContext verificationContext = CreateDbContext();
        bool persisted = await verificationContext.StudyAreas
            .AsNoTracking()
            .AnyAsync(item => item.Id == area.Id);
        Assert.False(persisted);
    }
}