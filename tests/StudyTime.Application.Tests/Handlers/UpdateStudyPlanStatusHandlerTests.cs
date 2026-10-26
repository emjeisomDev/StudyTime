using Moq;
using Xunit;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Application.StudyPlans.Commands;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.Tests.Handlers;

public sealed class UpdateStudyPlanStatusHandlerTests
{
    [Theory]
    [InlineData(StudyPlanStatus.Active, StudyPlanStatus.Inactive, "inactive")]
    [InlineData(StudyPlanStatus.Inactive, StudyPlanStatus.Active, "active")]
    public async Task UpdateStudyPlanStatusHandler_StatusValido_AlternaStatusERetornaDto(
        StudyPlanStatus initialStatus,
        StudyPlanStatus requestedStatus,
        string expectedStatus)
    {
        // Arrange
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        Guid id = Guid.NewGuid();
        StudyPlan plan = new(id, "Intensivo", new Coefficient(2.5m));
        plan.ChangeStatus(initialStatus);
        Mock<IStudyPlanRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        repository
            .Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        unitOfWork
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        UpdateStudyPlanStatusHandler handler = new(repository.Object, unitOfWork.Object);
        UpdateStudyPlanStatusCommand command = new(id, requestedStatus);

        // Act
        StudyPlanDto result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal("Intensivo", result.Name);
        Assert.Equal(2.5m, result.Coefficient);
        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(requestedStatus, plan.Status);
        repository.Verify(item => item.GetByIdAsync(id, cancellationToken), Times.Once);
        repository.Verify(item => item.Update(plan), Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(cancellationToken), Times.Once);
        unitOfWork.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStudyPlanStatusHandler_PlanoInexistente_LancaKeyNotFoundException()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Mock<IStudyPlanRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        repository
            .Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyPlan?)null);
        UpdateStudyPlanStatusHandler handler = new(repository.Object, unitOfWork.Object);
        UpdateStudyPlanStatusCommand command = new(id, StudyPlanStatus.Inactive);

        // Act
        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains(id.ToString(), exception.Message);
        Assert.Contains("was not found", exception.Message);
        repository.Verify(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(item => item.Update(It.IsAny<StudyPlan>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStudyPlanStatusHandler_StatusIndefinido_LancaDomainException()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        StudyPlan plan = new(id, "Intensivo", new Coefficient(2.5m));
        Mock<IStudyPlanRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        repository
            .Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        UpdateStudyPlanStatusHandler handler = new(repository.Object, unitOfWork.Object);
        UpdateStudyPlanStatusCommand command = new(id, (StudyPlanStatus)999);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("Invalid StudyPlan status", exception.Message);
        repository.Verify(item => item.Update(It.IsAny<StudyPlan>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStudyPlanStatusHandler_RequestNulo_LancaArgumentNullException()
    {
        // Arrange
        Mock<IStudyPlanRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        UpdateStudyPlanStatusHandler handler = new(repository.Object, unitOfWork.Object);

        // Act
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!, CancellationToken.None));

        // Assert
        Assert.Contains("request", exception.Message);
        repository.Verify(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
