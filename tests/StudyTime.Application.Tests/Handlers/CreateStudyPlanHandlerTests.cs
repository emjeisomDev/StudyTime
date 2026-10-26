using Moq;
using Xunit;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions;
using StudyTime.Application.StudyPlans.Commands;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.Tests.Handlers;

public sealed class CreateStudyPlanHandlerTests
{
    [Fact]
    public async Task CreateStudyPlanHandler_DadosValidos_CriaPlanoAtivoERetornaDto()
    {
        // Arrange
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        Mock<IStudyPlanRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        unitOfWork
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        CreateStudyPlanHandler handler = new(repository.Object, unitOfWork.Object);
        CreateStudyPlanCommand command = new("Intensivo", 2.5m);
        StudyPlan? addedPlan = null;
        repository
            .Setup(item => item.AddAsync(It.IsAny<StudyPlan>(), It.IsAny<CancellationToken>()))
            .Callback<StudyPlan, CancellationToken>((plan, _) => addedPlan = plan)
            .Returns(Task.CompletedTask);

        // Act
        StudyPlanDto result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Intensivo", result.Name);
        Assert.Equal(2.5m, result.Coefficient);
        Assert.Equal("active", result.Status);
        Assert.NotNull(addedPlan);
        Assert.Equal(StudyTime.Domain.Enums.StudyPlanStatus.Active, addedPlan!.Status);
        repository.Verify(
            item => item.AddAsync(
                It.Is<StudyPlan>(plan => plan.Name == "Intensivo" && plan.Coefficient.Value == 2.5m),
                cancellationToken),
            Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(cancellationToken), Times.Once);
        unitOfWork.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateStudyPlanHandler_RequestNulo_LancaArgumentNullException()
    {
        // Arrange
        Mock<IStudyPlanRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        CreateStudyPlanHandler handler = new(repository.Object, unitOfWork.Object);

        // Act
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!, CancellationToken.None));

        // Assert
        Assert.Contains("request", exception.Message);
        repository.Verify(item => item.AddAsync(It.IsAny<StudyPlan>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateStudyPlanHandler_CoeficienteNaoPositivo_LancaArgumentOutOfRangeException(decimal coefficient)
    {
        // Arrange
        Mock<IStudyPlanRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        CreateStudyPlanHandler handler = new(repository.Object, unitOfWork.Object);
        CreateStudyPlanCommand command = new("Intensivo", coefficient);

        // Act
        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("Coefficient must be greater than zero.", exception.Message);
        repository.Verify(item => item.AddAsync(It.IsAny<StudyPlan>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateStudyPlanHandler_NomeNulo_LancaArgumentNullException()
    {
        // Arrange
        Mock<IStudyPlanRepository> repository = new();
        Mock<IUnitOfWork> unitOfWork = new();
        CreateStudyPlanHandler handler = new(repository.Object, unitOfWork.Object);
        CreateStudyPlanCommand command = new(null!, 1.5m);

        // Act
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("name", exception.Message);
        repository.Verify(item => item.AddAsync(It.IsAny<StudyPlan>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
