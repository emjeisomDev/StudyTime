using Moq;
using Xunit;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Application.StudyAreas.Commands;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.Tests.Handlers;

public sealed class UpdateStudyAreaHandlerTests
{
    [Fact]
    public async Task UpdateStudyAreaHandler_NomeInformado_RetornaDtoEReconstruiEntidadeComMesmoId()
    {
        // Arrange
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        Guid id = Guid.NewGuid();
        StudyArea existingArea = new(id, "Nome anterior", new Minutes(600));
        StudyArea? updatedArea = null;
        Mock<IStudyAreaRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        repository
            .Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArea);
        repository
            .Setup(item => item.Update(It.IsAny<StudyArea>()))
            .Callback<StudyArea>(area => updatedArea = area);
        unitOfWork
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        UpdateStudyAreaHandler handler = new(repository.Object, unitOfWork.Object);
        UpdateStudyAreaCommand command = new(id, Name: "Nome novo");

        // Act
        StudyAreaDto result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal("Nome novo", result.Name);
        Assert.Equal(600, result.StdWeekStudyTime);
        Assert.NotNull(updatedArea);
        Assert.Equal(id, updatedArea!.Id);
        Assert.NotSame(existingArea, updatedArea);
        repository.Verify(item => item.GetByIdAsync(id, cancellationToken), Times.Once);
        repository.Verify(item => item.Update(updatedArea!), Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(cancellationToken), Times.Once);
        unitOfWork.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStudyAreaHandler_IdVazio_LancaArgumentException()
    {
        // Arrange
        Mock<IStudyAreaRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        UpdateStudyAreaHandler handler = new(repository.Object, unitOfWork.Object);
        UpdateStudyAreaCommand command = new(Guid.Empty, Name: "Nome novo");

        // Act
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("StudyArea id cannot be empty.", exception.Message);
        repository.Verify(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(item => item.Update(It.IsAny<StudyArea>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStudyAreaHandler_NenhumCampoInformado_LancaArgumentException()
    {
        // Arrange
        Mock<IStudyAreaRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        UpdateStudyAreaHandler handler = new(repository.Object, unitOfWork.Object);
        UpdateStudyAreaCommand command = new(Guid.NewGuid());

        // Act
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("At least one field must be provided for update.", exception.Message);
        repository.Verify(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(item => item.Update(It.IsAny<StudyArea>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStudyAreaHandler_AreaInexistente_LancaKeyNotFoundException()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Mock<IStudyAreaRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        repository
            .Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyArea?)null);
        UpdateStudyAreaHandler handler = new(repository.Object, unitOfWork.Object);
        UpdateStudyAreaCommand command = new(id, Name: "Nome novo");

        // Act
        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains(id.ToString(), exception.Message);
        Assert.Contains("was not found", exception.Message);
        repository.Verify(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(item => item.Update(It.IsAny<StudyArea>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStudyAreaHandler_MetaSemanalZero_LancaArgumentOutOfRangeException()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        StudyArea existingArea = new(id, "Matemática", new Minutes(600));
        Mock<IStudyAreaRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        repository
            .Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArea);
        UpdateStudyAreaHandler handler = new(repository.Object, unitOfWork.Object);
        UpdateStudyAreaCommand command = new(id, StdWeekStudyTime: 0);

        // Act
        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("Minutes must be greater than zero.", exception.Message);
        repository.Verify(item => item.Update(It.IsAny<StudyArea>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStudyAreaHandler_RequestNulo_LancaArgumentNullException()
    {
        // Arrange
        Mock<IStudyAreaRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        UpdateStudyAreaHandler handler = new(repository.Object, unitOfWork.Object);

        // Act
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!, CancellationToken.None));

        // Assert
        Assert.Contains("request", exception.Message);
        repository.Verify(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
