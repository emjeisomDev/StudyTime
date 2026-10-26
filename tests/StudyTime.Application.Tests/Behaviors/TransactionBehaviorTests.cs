using Moq;
using Xunit;
using MediatR;
using Microsoft.Extensions.Logging;
using StudyTime.Domain.Abstractions;
using StudyTime.Application.Behaviors;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.Tests.Behaviors;

public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task Handle_ComandoExecutadoComSucesso_AbreEConfirmaTransacao()
    {
        // Arrange
        Mock<IUnitOfWork> unitOfWork = CreateUnitOfWorkMock();
        Mock<ILogger<TransactionBehavior<FakeCommand, Unit>>> logger = new();
        TransactionBehavior<FakeCommand, Unit> behavior = new(unitOfWork.Object, logger.Object);
        FakeCommand request = new();
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        bool nextCalled = false;
        CancellationToken receivedToken = default;
        RequestHandlerDelegate<Unit> next = token =>
        {
            nextCalled = true;
            receivedToken = token;
            return Task.FromResult(Unit.Value);
        };

        // Act
        Unit result = await behavior.Handle(request, next, cancellationToken);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.True(nextCalled);
        Assert.Equal(cancellationToken, receivedToken);
        unitOfWork.Verify(x => x.BeginTransactionAsync(cancellationToken), Times.Once);
        unitOfWork.Verify(x => x.CommitTransactionAsync(cancellationToken), Times.Once);
        unitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_QueryExecutadaComSucesso_NaoAbreNemConfirmaTransacao()
    {
        // Arrange
        Mock<IUnitOfWork> unitOfWork = CreateUnitOfWorkMock();
        Mock<ILogger<TransactionBehavior<FakeQuery, Unit>>> logger = new();
        TransactionBehavior<FakeQuery, Unit> behavior = new(unitOfWork.Object, logger.Object);
        FakeQuery request = new();
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        bool nextCalled = false;
        CancellationToken receivedToken = default;
        RequestHandlerDelegate<Unit> next = token =>
        {
            nextCalled = true;
            receivedToken = token;
            return Task.FromResult(Unit.Value);
        };

        // Act
        Unit result = await behavior.Handle(request, next, cancellationToken);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.True(nextCalled);
        Assert.Equal(cancellationToken, receivedToken);
        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComandoComFalhaNoNext_ExecutaRollbackComCancellationTokenNoneERelancaExcecaoOriginal()
    {
        // Arrange
        Mock<IUnitOfWork> unitOfWork = CreateUnitOfWorkMock();
        Mock<ILogger<TransactionBehavior<FakeCommand, Unit>>> logger = new();
        TransactionBehavior<FakeCommand, Unit> behavior = new(unitOfWork.Object, logger.Object);
        FakeCommand request = new();
        InvalidOperationException originalException = new("Falha original do handler.");
        RequestHandlerDelegate<Unit> next = _ => Task.FromException<Unit>(originalException);

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        // Assert
        Assert.Same(originalException, exception);
        unitOfWork.Verify(x => x.BeginTransactionAsync(CancellationToken.None), Times.Once);
        unitOfWork.Verify(x => x.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RollbackFalha_PreservaExcecaoOriginalERegistraErro()
    {
        // Arrange
        Mock<IUnitOfWork> unitOfWork = CreateUnitOfWorkMock();
        InvalidOperationException rollbackException = new("Falha no rollback.");
        unitOfWork
            .Setup(x => x.RollbackTransactionAsync(CancellationToken.None))
            .ThrowsAsync(rollbackException);

        Mock<ILogger<TransactionBehavior<FakeCommand, Unit>>> logger = new();
        TransactionBehavior<FakeCommand, Unit> behavior = new(unitOfWork.Object, logger.Object);
        FakeCommand request = new();
        InvalidOperationException originalException = new("Falha original do handler.");
        RequestHandlerDelegate<Unit> next = _ => Task.FromException<Unit>(originalException);

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        // Assert
        Assert.Same(originalException, exception);
        unitOfWork.Verify(x => x.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Rollback failed", StringComparison.Ordinal)),
                rollbackException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        Mock<IUnitOfWork> unitOfWork = new();
        unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return unitOfWork;
    }

    // Public access is required because Moq/Castle generates a proxy for
    // ILogger<TransactionBehavior<TRequest, TResponse>> using these generic types.
    public sealed record FakeCommand : ICommand<Unit>;

    public sealed record FakeQuery : IRequest<Unit>;
}
