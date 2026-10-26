using Moq;
using Xunit;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions;
using StudyTime.Application.StudyAreas.Commands;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.Tests.Handlers;

public sealed class CreateStudyAreaHandlerTests
{
    [Fact]
    public async Task CreateStudyAreaHandler_DadosValidos_RetornaDtoEAdicionaArea()
    {
        // Arrange
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        Mock<IStudyAreaRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        repository
            .Setup(item => item.AddAsync(It.IsAny<StudyArea>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        CreateStudyAreaHandler handler = new(repository.Object, unitOfWork.Object);
        CreateStudyAreaCommand command = new("Matemática", 600);

        // Act
        StudyAreaDto result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Matemática", result.Name);
        Assert.Equal(600, result.StdWeekStudyTime);
        repository.Verify(
            item => item.AddAsync(
                It.Is<StudyArea>(area => area.Name == "Matemática" && area.StdWeekStudyTime.Value == 600),
                cancellationToken),
            Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(cancellationToken), Times.Once);
        unitOfWork.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateStudyAreaHandler_RequestNulo_LancaArgumentNullException()
    {
        // Arrange
        Mock<IStudyAreaRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        CreateStudyAreaHandler handler = new(repository.Object, unitOfWork.Object);

        // Act
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!, CancellationToken.None));

        // Assert
        Assert.Contains("request", exception.Message);
        repository.Verify(
            item => item.AddAsync(It.IsAny<StudyArea>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateStudyAreaHandler_MinutosNaoPositivos_LancaArgumentOutOfRangeException(int minutes)
    {
        // Arrange
        Mock<IStudyAreaRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        CreateStudyAreaHandler handler = new(repository.Object, unitOfWork.Object);
        CreateStudyAreaCommand command = new("Matemática", minutes);

        // Act
        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("Minutes must be greater than zero.", exception.Message);
        repository.Verify(
            item => item.AddAsync(It.IsAny<StudyArea>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateStudyAreaHandler_NomeNulo_LancaArgumentNullException()
    {
        // Arrange
        Mock<IStudyAreaRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        CreateStudyAreaHandler handler = new(repository.Object, unitOfWork.Object);
        CreateStudyAreaCommand command = new(null!, 600);

        // Act
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("name", exception.Message);
        repository.Verify(
            item => item.AddAsync(It.IsAny<StudyArea>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
