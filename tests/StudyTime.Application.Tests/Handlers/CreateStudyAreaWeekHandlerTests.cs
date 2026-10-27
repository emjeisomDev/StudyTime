using Moq;
using Xunit;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Application.StudyAreaWeeks.Commands;

namespace StudyTime.Application.Tests.Handlers;

public sealed class CreateStudyAreaWeekHandlerTests
{
    [Fact]
    public async Task Handle_ConfiguracaoValida_CriaAvaliacaoConfiguracaoEUmUnicoSaveChanges()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        // Act
        StudyAreaWeekDto result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(fixture.TargetWeekStart, result.WeekStartDate);
        Assert.Equal(fixture.TargetStudyArea.Id, result.StudyAreaId);
        Assert.Equal(fixture.TargetStudyPlan.Id, result.StudyPlanId);
        Assert.Equal(1500m, result.WeekIndividualGoal);
        Assert.Equal(0, result.MinutesStudied);
        Assert.False(result.GoalAchieved);

        WeeklyAssessment addedWeeklyAssessment = Assert.Single(fixture.AddedWeeklyAssessments);
        Assert.Equal(1500m, addedWeeklyAssessment.WeekGlobalGoal);
        StudyAreaWeek addedWeek = Assert.Single(fixture.AddedStudyAreaWeeks);
        StudyAreaWeekAssessment addedAssessment = Assert.Single(fixture.AddedStudyAreaWeekAssessments);
        Assert.Equal(addedWeek.Id, addedAssessment.StudyAreaWeekId);
        Assert.Equal(addedWeeklyAssessment.Id, result.WeeklyAssessmentId);
        Assert.Equal(
            new[]
            {
                "WeeklyAssessment.Add",
                "StudyAreaWeek.Add",
                "StudyAreaWeekAssessment.Add",
                "UnitOfWork.SaveChanges"
            },
            fixture.CallOrder);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_AvaliacaoSemanalJaExistente_ReutilizaAvaliacaoERecalculaMetaGlobal()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        StudyArea existingArea = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Área já configurada", 600);
        StudyArea newArea = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Área nova", 900);
        fixture.AddStudyArea(existingArea);
        fixture.AddStudyArea(newArea);
        WeeklyAssessment existingWeeklyAssessment =
            WeeklyConfigurationHandlerTestFixture.CreateWeeklyAssessment(fixture.TargetWeekStart, 600m);
        StudyAreaWeek existingWeek = new(
            fixture.TargetWeekStart,
            existingArea.Id,
            fixture.TargetStudyPlan.Id,
            existingWeeklyAssessment.Id);
        fixture.SetStudyAreaWeeks(fixture.TargetWeekStart, existingWeek);
        fixture.SetStudyAreaWeekAssessment(new StudyAreaWeekAssessment(600m, existingWeek.Id));
        fixture.SetWeeklyAssessment(fixture.TargetWeekStart, existingWeeklyAssessment);

        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(newArea.Id, fixture.TargetStudyPlan.Id, fixture.TargetWeekStart);

        // Act
        StudyAreaWeekDto result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(existingWeeklyAssessment.Id, result.WeeklyAssessmentId);
        Assert.Equal(900m, result.WeekIndividualGoal);
        Assert.Equal(1500m, existingWeeklyAssessment.WeekGlobalGoal);
        fixture.WeeklyAssessmentRepository.Verify(
            item => item.AddAsync(It.IsAny<WeeklyAssessment>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.WeeklyAssessmentRepository.Verify(item => item.Update(existingWeeklyAssessment), Times.Once);
        Assert.Empty(fixture.AddedWeeklyAssessments);
        Assert.Single(fixture.AddedStudyAreaWeeks);
        Assert.Single(fixture.AddedStudyAreaWeekAssessments);
        Assert.Equal(
            new[]
            {
                "WeeklyAssessment.Update",
                "StudyAreaWeek.Add",
                "StudyAreaWeekAssessment.Add",
                "UnitOfWork.SaveChanges"
            },
            fixture.CallOrder);
    }

    [Theory]
    [InlineData(1499, false)]
    [InlineData(1500, true)]
    public async Task Handle_MetaGlobalNoLimiteMinimo_AplicaRegraDe1500Minutos(int globalGoal, bool succeeds)
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        StudyArea area = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Área limite", globalGoal);
        fixture.AddStudyArea(area);
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(area.Id, fixture.TargetStudyPlan.Id, fixture.TargetWeekStart);

        // Act
        if (!succeeds)
        {
            WeeklyGoalNotMetException exception = await Assert.ThrowsAsync<WeeklyGoalNotMetException>(
                () => handler.Handle(command, CancellationToken.None));

            // Assert
            Assert.Contains("at least 1500 minutes", exception.Message);
            Assert.Empty(fixture.AddedWeeklyAssessments);
            Assert.Empty(fixture.AddedStudyAreaWeeks);
            fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            return;
        }

        StudyAreaWeekDto result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1500m, result.WeekIndividualGoal);
        Assert.Equal(1500m, Assert.Single(fixture.AddedWeeklyAssessments).WeekGlobalGoal);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_SemanaNaoIniciadaNaSegunda_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart.AddDays(1));

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("must start on a Monday", exception.Message);
        Assert.Empty(fixture.CallOrder);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SemanaForaDaJanelaPermitida_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            WeeklyConfigurationHandlerTestFixture.CurrentWeekStart.AddDays(35));

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("current week or one of the four following weeks", exception.Message);
        Assert.Empty(fixture.CallOrder);
        fixture.WeeklyAssessmentRepository.Verify(
            item => item.GetByYearAndWeekNumberAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SemanaCorrenteNaoConfigurada_LancaWeekConfigurationLockedException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        fixture.WeeklyAssessmentsByIsoWeek.Remove(
            WeeklyConfigurationHandlerTestFixture.ToIsoKey(WeeklyConfigurationHandlerTestFixture.CurrentWeekStart));
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        WeekConfigurationLockedException exception = await Assert.ThrowsAsync<WeekConfigurationLockedException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("current week is not configured", exception.Message);
        fixture.StudyAreaRepository.Verify(
            item => item.GetByIdAsync(fixture.TargetStudyArea.Id, It.IsAny<CancellationToken>()), Times.Never);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConfiguracaoCorrenteComWeeklyAssessmentDivergente_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        StudyAreaWeek inconsistentCurrentWeek = new(
            WeeklyConfigurationHandlerTestFixture.CurrentWeekStart,
            fixture.CurrentStudyArea.Id,
            fixture.CurrentStudyPlan.Id,
            Guid.NewGuid());
        fixture.SetStudyAreaWeeks(WeeklyConfigurationHandlerTestFixture.CurrentWeekStart, inconsistentCurrentWeek);
        fixture.SetStudyAreaWeekAssessment(new StudyAreaWeekAssessment(1500m, inconsistentCurrentWeek.Id, 1500));
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("must reference its WeeklyAssessment", exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConfiguracaoCorrenteSemAvaliacaoIndividual_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        fixture.StudyAreaWeekAssessmentsByWeekId.Remove(fixture.CurrentStudyAreaWeek.Id);
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("StudyAreaWeekAssessment was not found", exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MetaIndividualDaSemanaCorrenteNaoAtingida_LancaWeekConfigurationLockedException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        fixture.CurrentStudyAreaWeekAssessment.UpdateMinutesStudied(1499);
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        WeekConfigurationLockedException exception = await Assert.ThrowsAsync<WeekConfigurationLockedException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("after every individual goal", exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AreaInexistente_LancaKeyNotFoundException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        fixture.StudyAreasById.Remove(fixture.TargetStudyArea.Id);
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("was not found", exception.Message);
        fixture.StudyPlanRepository.Verify(
            item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PlanoInexistente_LancaKeyNotFoundException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        fixture.StudyPlansById.Remove(fixture.TargetStudyPlan.Id);
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("was not found", exception.Message);
        fixture.StudyAreaWeekRepository.Verify(
            item => item.ExistsForStudyAreaInWeekAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PlanoInativo_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        fixture.TargetStudyPlan.ChangeStatus(StudyPlanStatus.Inactive);
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("inactive StudyPlan", exception.Message);
        fixture.StudyAreaWeekRepository.Verify(
            item => item.ExistsForStudyAreaInWeekAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AreaJaConfiguradaNaSemana_LancaDuplicateStudyAreaWeekException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        StudyAreaWeek existingWeek = new(
            fixture.TargetWeekStart,
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            Guid.NewGuid());
        fixture.SetStudyAreaWeeks(fixture.TargetWeekStart, existingWeek);
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        DuplicateStudyAreaWeekException exception = await Assert.ThrowsAsync<DuplicateStudyAreaWeekException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("already configured", exception.Message);
        fixture.StudyAreaWeekRepository.Verify(
            item => item.AddAsync(It.IsAny<StudyAreaWeek>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConfiguracaoComAvaliacaoSemanalDivergente_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        StudyAreaWeek existingWeek = new(
            fixture.TargetWeekStart,
            WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Área anterior", 300).Id,
            fixture.TargetStudyPlan.Id,
            Guid.NewGuid());
        fixture.SetStudyAreaWeeks(fixture.TargetWeekStart, existingWeek);
        fixture.SetStudyAreaWeekAssessment(new StudyAreaWeekAssessment(300m, existingWeek.Id));
        fixture.SetWeeklyAssessment(
            fixture.TargetWeekStart,
            WeeklyConfigurationHandlerTestFixture.CreateWeeklyAssessment(fixture.TargetWeekStart, 300m));
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("must reference the same WeeklyAssessment", exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConfiguracaoExistenteSemAvaliacaoIndividual_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        StudyArea existingArea = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Área anterior", 300);
        fixture.AddStudyArea(existingArea);
        StudyAreaWeek existingWeek = new(
            fixture.TargetWeekStart,
            existingArea.Id,
            fixture.TargetStudyPlan.Id,
            Guid.NewGuid());
        fixture.SetStudyAreaWeeks(fixture.TargetWeekStart, existingWeek);
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("StudyAreaWeekAssessment was not found", exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConfiguracoesExistentesSemAvaliacaoSemanal_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        StudyArea existingArea = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Área anterior", 300);
        fixture.AddStudyArea(existingArea);
        StudyAreaWeek existingWeek = new(
            fixture.TargetWeekStart,
            existingArea.Id,
            fixture.TargetStudyPlan.Id,
            Guid.NewGuid());
        fixture.SetStudyAreaWeeks(fixture.TargetWeekStart, existingWeek);
        fixture.SetStudyAreaWeekAssessment(new StudyAreaWeekAssessment(300m, existingWeek.Id));
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("WeeklyAssessment is missing", exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AvaliacaoSemanalSemConfiguracoes_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        fixture.SetWeeklyAssessment(
            fixture.TargetWeekStart,
            WeeklyConfigurationHandlerTestFixture.CreateWeeklyAssessment(fixture.TargetWeekStart, 1500m));
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            fixture.TargetWeekStart);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("exists for a week without StudyAreaWeek", exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RequestComIdentificadorVazio_LancaArgumentExceptionAntesDeConsultarRepositorios()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        CreateStudyAreaWeekHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeekCommand command = new(Guid.Empty, fixture.TargetStudyPlan.Id, fixture.TargetWeekStart);

        // Act
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("StudyAreaId is required", exception.Message);
        fixture.CurrentWeekProvider.Verify(item => item.GetCurrentWeek(), Times.Never);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CreateStudyAreaWeekHandler CreateHandler(WeeklyConfigurationHandlerTestFixture fixture)
    {
        return new CreateStudyAreaWeekHandler(
            fixture.UnitOfWork.Object,
            fixture.IsoWeekCalendar.Object,
            fixture.StudyAreaRepository.Object,
            fixture.StudyPlanRepository.Object,
            fixture.CurrentWeekProvider.Object,
            fixture.StudyAreaWeekRepository.Object,
            fixture.WeeklyAssessmentRepository.Object,
            fixture.StudyAreaWeekAssessmentRepository.Object);
    }
}
