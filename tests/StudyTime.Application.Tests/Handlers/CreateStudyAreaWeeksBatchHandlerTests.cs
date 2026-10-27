using Moq;
using Xunit;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Application.StudyAreaWeeks.Commands;

namespace StudyTime.Application.Tests.Handlers;

public sealed class CreateStudyAreaWeeksBatchHandlerTests
{
    [Fact]
    public async Task Handle_DuasAreasValidas_PersisteTudoComUmaUnicaChamadaSaveChanges()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        StudyArea mathematics = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Matemática", 900);
        StudyArea physics = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Física", 600);
        fixture.AddStudyArea(mathematics);
        fixture.AddStudyArea(physics);
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[]
            {
                new StudyAreaWeekBatchItemDto(mathematics.Id, fixture.TargetStudyPlan.Id),
                new StudyAreaWeekBatchItemDto(physics.Id, fixture.TargetStudyPlan.Id)
            });
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        // Act
        IReadOnlyList<StudyAreaWeekDto> result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.StudyAreaId == mathematics.Id && item.WeekIndividualGoal == 900m);
        Assert.Contains(result, item => item.StudyAreaId == physics.Id && item.WeekIndividualGoal == 600m);
        Assert.All(result, item => Assert.Equal(fixture.TargetWeekStart, item.WeekStartDate));
        Assert.All(result, item => Assert.False(item.GoalAchieved));
        WeeklyAssessment weeklyAssessment = Assert.Single(fixture.AddedWeeklyAssessments);
        Assert.Equal(1500m, weeklyAssessment.WeekGlobalGoal);
        Assert.Equal(2, fixture.AddedStudyAreaWeeks.Count);
        Assert.Equal(2, fixture.AddedStudyAreaWeekAssessments.Count);
        Assert.All(fixture.AddedStudyAreaWeeks, item => Assert.Equal(weeklyAssessment.Id, item.WeeklyAssessmentId));
        Assert.Equal(
            new[]
            {
                "WeeklyAssessment.Add",
                "StudyAreaWeek.Add",
                "StudyAreaWeekAssessment.Add",
                "StudyAreaWeek.Add",
                "StudyAreaWeekAssessment.Add",
                "UnitOfWork.SaveChanges"
            },
            fixture.CallOrder);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(cancellationToken), Times.Once);
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
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(area.Id, fixture.TargetStudyPlan.Id) });

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

        IReadOnlyList<StudyAreaWeekDto> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(1500m, Assert.Single(fixture.AddedWeeklyAssessments).WeekGlobalGoal);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ListaVazia_LancaArgumentExceptionAntesDeConsultarSemana()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(fixture.TargetWeekStart, Array.Empty<StudyAreaWeekBatchItemDto>());

        // Act
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("At least one item is required", exception.Message);
        fixture.CurrentWeekProvider.Verify(item => item.GetCurrentWeek(), Times.Never);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AreaRepetidaNoMesmoLote_LancaDuplicateStudyAreaWeekException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[]
            {
                new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id),
                new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id)
            });

        // Act
        DuplicateStudyAreaWeekException exception = await Assert.ThrowsAsync<DuplicateStudyAreaWeekException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("occurs more than once", exception.Message);
        Assert.Empty(fixture.AddedStudyAreaWeeks);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AreaJaExistenteNaSemanaSolicitada_LancaDuplicateStudyAreaWeekException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        StudyAreaWeek existingWeek = new(
            fixture.TargetWeekStart,
            fixture.TargetStudyArea.Id,
            fixture.TargetStudyPlan.Id,
            Guid.NewGuid());
        fixture.SetStudyAreaWeeks(fixture.TargetWeekStart, existingWeek);
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

        // Act
        DuplicateStudyAreaWeekException exception = await Assert.ThrowsAsync<DuplicateStudyAreaWeekException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("already configured", exception.Message);
        Assert.Empty(fixture.AddedStudyAreaWeeks);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SemanaNaoIniciadaNaSegunda_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart.AddDays(1),
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

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
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            WeeklyConfigurationHandlerTestFixture.CurrentWeekStart.AddDays(35),
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("current week or one of the four following weeks", exception.Message);
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
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

        // Act
        WeekConfigurationLockedException exception = await Assert.ThrowsAsync<WeekConfigurationLockedException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("current week is not configured", exception.Message);
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
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

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
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

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
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

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
        Guid unknownAreaId = Guid.NewGuid();
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(unknownAreaId, fixture.TargetStudyPlan.Id) });

        // Act
        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains(unknownAreaId.ToString(), exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PlanoInexistente_LancaKeyNotFoundException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        Guid unknownPlanId = Guid.NewGuid();
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, unknownPlanId) });

        // Act
        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains(unknownPlanId.ToString(), exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PlanoInativo_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        fixture.TargetStudyPlan.ChangeStatus(StudyPlanStatus.Inactive);
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("inactive StudyPlan", exception.Message);
        Assert.Empty(fixture.AddedStudyAreaWeeks);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AvaliacaoDeAreaExistenteAusente_LancaDomainException()
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
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

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
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("WeeklyAssessment is missing", exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConfiguracaoComAvaliacaoSemanalDivergente_LancaDomainException()
    {
        // Arrange
        WeeklyConfigurationHandlerTestFixture fixture = new();
        StudyArea existingArea = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Área anterior", 300);
        fixture.AddStudyArea(existingArea);
        WeeklyAssessment targetWeeklyAssessment =
            WeeklyConfigurationHandlerTestFixture.CreateWeeklyAssessment(fixture.TargetWeekStart, 300m);
        StudyAreaWeek existingWeek = new(
            fixture.TargetWeekStart,
            existingArea.Id,
            fixture.TargetStudyPlan.Id,
            Guid.NewGuid());
        fixture.SetStudyAreaWeeks(fixture.TargetWeekStart, existingWeek);
        fixture.SetStudyAreaWeekAssessment(new StudyAreaWeekAssessment(300m, existingWeek.Id));
        fixture.SetWeeklyAssessment(fixture.TargetWeekStart, targetWeeklyAssessment);
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("must reference the same WeeklyAssessment", exception.Message);
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
        CreateStudyAreaWeeksBatchHandler handler = CreateHandler(fixture);
        CreateStudyAreaWeeksBatchCommand command = new(
            fixture.TargetWeekStart,
            new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("exists for a week without StudyAreaWeek", exception.Message);
        fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CreateStudyAreaWeeksBatchHandler CreateHandler(WeeklyConfigurationHandlerTestFixture fixture)
    {
        return new CreateStudyAreaWeeksBatchHandler(
            fixture.UnitOfWork.Object,
            fixture.IsoWeekCalendar.Object,
            fixture.CurrentWeekProvider.Object,
            fixture.StudyAreaRepository.Object,
            fixture.StudyPlanRepository.Object,
            fixture.StudyAreaWeekRepository.Object,
            fixture.WeeklyAssessmentRepository.Object,
            fixture.StudyAreaWeekAssessmentRepository.Object);
    }
}
