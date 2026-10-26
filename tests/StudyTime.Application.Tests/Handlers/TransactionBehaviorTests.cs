using Moq;
using Xunit;
using MediatR;
using StudyTime.Application.Dtos;
using Microsoft.Extensions.Logging;
using StudyTime.Domain.Abstractions;
using StudyTime.Application.Behaviors;
using StudyTime.Application.StudyAreas.Commands;

namespace StudyTime.Application.Tests.Handlers;

public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task Handle_ComandoConcluido_ComportamentoRealizaCommit()
    {
        // Arrange
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        Mock<IUnitOfWork> unitOfWork = new();
        unitOfWork
            .Setup(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ILogger<TransactionBehavior<CreateStudyAreaCommand, StudyAreaDto>>> logger = new();
        TransactionBehavior<CreateStudyAreaCommand, StudyAreaDto> behavior = new(
            unitOfWork.Object,
            logger.Object);
        CreateStudyAreaCommand command = new("Matemática", 600);
        StudyAreaDto expected = new(Guid.NewGuid(), "Matemática", 600);
        RequestHandlerDelegate<StudyAreaDto> next = _ => Task.FromResult(expected);

        // Act
        StudyAreaDto result = await behavior.Handle(command, next, cancellationToken);

        // Assert
        Assert.Equal(expected, result);
        unitOfWork.Verify(item => item.BeginTransactionAsync(cancellationToken), Times.Once);
        unitOfWork.Verify(item => item.CommitTransactionAsync(cancellationToken), Times.Once);
        unitOfWork.Verify(item => item.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComandoFalha_ExecutaRollbackERetornaExcecaoOriginal()
    {
        // Arrange
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        Mock<IUnitOfWork> unitOfWork = new();
        unitOfWork
            .Setup(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(item => item.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ILogger<TransactionBehavior<CreateStudyAreaCommand, StudyAreaDto>>> logger = new();
        TransactionBehavior<CreateStudyAreaCommand, StudyAreaDto> behavior = new(
            unitOfWork.Object,
            logger.Object);
        CreateStudyAreaCommand command = new("Matemática", 600);
        InvalidOperationException expectedException = new("Falha simulada no handler.");
        RequestHandlerDelegate<StudyAreaDto> next = _ => Task.FromException<StudyAreaDto>(expectedException);

        // Act
        InvalidOperationException actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(command, next, cancellationToken));

        // Assert
        Assert.Same(expectedException, actualException);
        unitOfWork.Verify(item => item.BeginTransactionAsync(cancellationToken), Times.Once);
        unitOfWork.Verify(item => item.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        unitOfWork.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
