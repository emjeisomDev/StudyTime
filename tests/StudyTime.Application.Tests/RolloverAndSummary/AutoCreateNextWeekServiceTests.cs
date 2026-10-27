using Moq;
using Xunit;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.StudyAreaWeeks.AutoCreation;

namespace StudyTime.Application.Tests.RolloverAndSummary;

public sealed class AutoCreateNextWeekServiceTests
{
    private static readonly DateOnly CurrentWeekStart = new(2026, 10, 19);
    private static readonly DateOnly NextWeekStart = new(2026, 10, 26);

    [Fact]
    public async Task ExecuteAsync_DomingoComDuasConfiguracoes_CriaConfiguracoesNaSemanaIsoSeguinte()
    {
        // Arrange
        TestContext context = new(CreateSundayUtcClock());

        // Act
        await context.Service.ExecuteAsync();

        // Assert
        Assert.Single(context.AddedWeeklyAssessments);
        WeeklyAssessment addedWeeklyAssessment = context.AddedWeeklyAssessments[0];
        Assert.Equal(44, addedWeeklyAssessment.WeekNumber);
        Assert.Equal(2026, addedWeeklyAssessment.Year);
        Assert.Equal(1500m, addedWeeklyAssessment.WeekGlobalGoal);

        Assert.Equal(context.CurrentStudyAreaWeeks.Count, context.AddedStudyAreaWeeks.Count);
        Assert.Equal(context.CurrentStudyAreaWeeks.Count, context.AddedIndividualAssessments.Count);
        Assert.All(context.AddedStudyAreaWeeks, item => Assert.Equal(NextWeekStart, item.WeekStartDate));
        Assert.All(context.AddedStudyAreaWeeks, item => Assert.Equal(addedWeeklyAssessment.Id, item.WeeklyAssessmentId));
        Assert.All(context.AddedIndividualAssessments, assessment =>
            Assert.Contains(context.AddedStudyAreaWeeks, item => item.Id == assessment.StudyAreaWeekId));
        Assert.All(context.AddedIndividualAssessments, assessment => Assert.Equal(750m, assessment.WeekIndividualGoal));

        for (int index = 0; index < context.CurrentStudyAreaWeeks.Count; index++)
        {
            StudyAreaWeek source = context.CurrentStudyAreaWeeks[index];
            StudyAreaWeek copy = Assert.Single(context.AddedStudyAreaWeeks,
                item => item.StudyAreaId == source.StudyAreaId);

            Assert.NotEqual(source.Id, copy.Id);
            Assert.Equal(source.StudyAreaId, copy.StudyAreaId);
            Assert.Equal(source.StudyPlanId, copy.StudyPlanId);
        }

        context.WeeklyAssessmentRepository.Verify(
            repository => repository.AddAsync(It.IsAny<WeeklyAssessment>(), It.IsAny<CancellationToken>()),
            Times.Once);
        context.StudyAreaWeekRepository.Verify(
            repository => repository.AddAsync(It.IsAny<StudyAreaWeek>(), It.IsAny<CancellationToken>()),
            Times.Exactly(context.CurrentStudyAreaWeeks.Count));
        context.StudyAreaWeekAssessmentRepository.Verify(
            repository => repository.AddAsync(It.IsAny<StudyAreaWeekAssessment>(), It.IsAny<CancellationToken>()),
            Times.Exactly(context.CurrentStudyAreaWeeks.Count));
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_SegundaFeiraLocal_ConfirmaTransacaoSemConsultarConfiguracoes()
    {
        // Arrange
        // Monday, 26/10/2026 at 12:00 in America/Sao_Paulo (15:00 UTC).
        TestContext context = new(new DateTime(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc));

        // Act
        await context.Service.ExecuteAsync();

        // Assert
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.CurrentWeekProvider.Verify(provider => provider.GetCurrentWeek(), Times.Never);
        context.StudyAreaWeekRepository.Verify(
            repository => repository.GetByWeekStartDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Empty(context.AddedWeeklyAssessments);
        Assert.Empty(context.AddedStudyAreaWeeks);
        Assert.Empty(context.AddedIndividualAssessments);
    }

    [Fact]
    public async Task ExecuteAsync_DomingoQuandoSemanaSeguinteJaPossuiConfiguracao_NaoReplica()
    {
        // Arrange
        TestContext context = new(CreateSundayUtcClock());
        StudyAreaWeek alreadyConfigured = new(
            NextWeekStart,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
        context.StudyAreaWeekRepository
            .Setup(repository => repository.GetByWeekStartDateAsync(NextWeekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StudyAreaWeek>)new[] { alreadyConfigured });

        // Act
        await context.Service.ExecuteAsync();

        // Assert
        context.StudyAreaWeekRepository.Verify(
            repository => repository.GetByWeekStartDateAsync(CurrentWeekStart, It.IsAny<CancellationToken>()),
            Times.Never);
        context.WeeklyAssessmentRepository.Verify(
            repository => repository.AddAsync(It.IsAny<WeeklyAssessment>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(context.AddedStudyAreaWeeks);
    }

    [Fact]
    public async Task ExecuteAsync_DomingoSemConfiguracaoNaSemanaCorrente_NaoReplica()
    {
        // Arrange
        TestContext context = new(CreateSundayUtcClock());
        context.StudyAreaWeekRepository
            .Setup(repository => repository.GetByWeekStartDateAsync(CurrentWeekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StudyAreaWeek>)Array.Empty<StudyAreaWeek>());

        // Act
        await context.Service.ExecuteAsync();

        // Assert
        context.WeeklyAssessmentRepository.Verify(
            repository => repository.GetByYearAndWeekNumberAsync(2026, 43, It.IsAny<CancellationToken>()),
            Times.Never);
        context.WeeklyAssessmentRepository.Verify(
            repository => repository.AddAsync(It.IsAny<WeeklyAssessment>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(context.AddedStudyAreaWeeks);
    }

    [Fact]
    public async Task ExecuteAsync_DomingoComPlanoInativo_FazRollbackERelancaDomainException()
    {
        // Arrange
        TestContext context = new(CreateSundayUtcClock());
        context.StudyPlans[0].ChangeStatus(StudyPlanStatus.Inactive);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => context.Service.ExecuteAsync());

        // Assert
        Assert.Contains("inactive", exception.Message, StringComparison.OrdinalIgnoreCase);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.WeeklyAssessmentRepository.Verify(
            repository => repository.AddAsync(It.IsAny<WeeklyAssessment>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Empty(context.AddedStudyAreaWeeks);
        Assert.Empty(context.AddedIndividualAssessments);
    }

    [Fact]
    public async Task ExecuteAsync_DomingoSemAvaliacaoDaSemanaCorrente_FazRollbackERelancaDomainException()
    {
        // Arrange
        TestContext context = new(CreateSundayUtcClock());
        context.WeeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(2026, 43, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => context.Service.ExecuteAsync());

        // Assert
        Assert.Contains("ISO WeeklyAssessment", exception.Message, StringComparison.Ordinal);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_DomingoComAvaliacaoDaProximaSemanaSemConfiguracoes_FazRollback()
    {
        // Arrange
        TestContext context = new(CreateSundayUtcClock());
        context.WeeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(2026, 44, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeeklyAssessment(44, 2026, 1500m));

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => context.Service.ExecuteAsync());

        // Assert
        Assert.Contains("exists for the next week", exception.Message, StringComparison.Ordinal);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(context.AddedWeeklyAssessments);
    }

    // Sunday, 25/10/2026 at 23:30 in America/Sao_Paulo (02:30 UTC on Monday).
    private static DateTime CreateSundayUtcClock() =>
        new(2026, 10, 26, 2, 30, 0, DateTimeKind.Utc);

    private sealed class FakeClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    private sealed class TestContext
    {
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public Mock<IIsoWeekCalendar> IsoWeekCalendar { get; } = new();
        public Mock<ICurrentWeekProvider> CurrentWeekProvider { get; } = new();
        public Mock<IStudyAreaRepository> StudyAreaRepository { get; } = new();
        public Mock<IStudyPlanRepository> StudyPlanRepository { get; } = new();
        public Mock<IStudyAreaWeekRepository> StudyAreaWeekRepository { get; } = new();
        public Mock<IWeeklyAssessmentRepository> WeeklyAssessmentRepository { get; } = new();
        public Mock<IStudyAreaWeekAssessmentRepository> StudyAreaWeekAssessmentRepository { get; } = new();
        public List<WeeklyAssessment> AddedWeeklyAssessments { get; } = [];
        public List<StudyAreaWeek> AddedStudyAreaWeeks { get; } = [];
        public List<StudyAreaWeekAssessment> AddedIndividualAssessments { get; } = [];
        public StudyPlan[] StudyPlans { get; }
        public IReadOnlyList<StudyAreaWeek> CurrentStudyAreaWeeks { get; }
        public AutoCreateNextWeekService Service { get; }

        public TestContext(DateTime utcNow)
        {
            FakeClock clock = new(utcNow);

            StudyArea[] areas =
            [
                new StudyArea(Guid.NewGuid(), "Matemática", new Minutes(750)),
                new StudyArea(Guid.NewGuid(), "Física", new Minutes(750))
            ];
            StudyPlans =
            [
                new StudyPlan(Guid.NewGuid(), "Plano A", new Coefficient(1m)),
                new StudyPlan(Guid.NewGuid(), "Plano B", new Coefficient(1m))
            ];

            WeeklyAssessment currentAssessment = new(Guid.NewGuid(), 43, 2026, 1500m);
            CurrentStudyAreaWeeks =
            [
                new StudyAreaWeek(CurrentWeekStart, areas[0].Id, StudyPlans[0].Id, currentAssessment.Id),
                new StudyAreaWeek(CurrentWeekStart, areas[1].Id, StudyPlans[1].Id, currentAssessment.Id)
            ];

            UnitOfWork.Setup(unitOfWork => unitOfWork.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            UnitOfWork.Setup(unitOfWork => unitOfWork.CommitTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            UnitOfWork.Setup(unitOfWork => unitOfWork.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            UnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            CurrentWeekProvider.Setup(provider => provider.GetCurrentWeek())
                .Returns(new WeekRange(CurrentWeekStart));

            IsoWeekCalendar.Setup(calendar => calendar.GetIsoWeek(CurrentWeekStart))
                .Returns(new IsoWeek(2026, 43));
            IsoWeekCalendar.Setup(calendar => calendar.GetIsoWeek(NextWeekStart))
                .Returns(new IsoWeek(2026, 44));

            StudyAreaWeekRepository
                .Setup(repository => repository.GetByWeekStartDateAsync(NextWeekStart, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<StudyAreaWeek>)Array.Empty<StudyAreaWeek>());
            StudyAreaWeekRepository
                .Setup(repository => repository.GetByWeekStartDateAsync(CurrentWeekStart, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CurrentStudyAreaWeeks);
            StudyAreaWeekRepository
                .Setup(repository => repository.AddAsync(It.IsAny<StudyAreaWeek>(), It.IsAny<CancellationToken>()))
                .Callback<StudyAreaWeek, CancellationToken>((item, _) => AddedStudyAreaWeeks.Add(item))
                .Returns(Task.CompletedTask);
            StudyAreaWeekAssessmentRepository
                .Setup(repository => repository.AddAsync(It.IsAny<StudyAreaWeekAssessment>(), It.IsAny<CancellationToken>()))
                .Callback<StudyAreaWeekAssessment, CancellationToken>((item, _) => AddedIndividualAssessments.Add(item))
                .Returns(Task.CompletedTask);

            WeeklyAssessmentRepository
                .Setup(repository => repository.GetByYearAndWeekNumberAsync(2026, 43, It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentAssessment);
            WeeklyAssessmentRepository
                .Setup(repository => repository.GetByYearAndWeekNumberAsync(2026, 44, It.IsAny<CancellationToken>()))
                .ReturnsAsync((WeeklyAssessment?)null);
            WeeklyAssessmentRepository
                .Setup(repository => repository.AddAsync(It.IsAny<WeeklyAssessment>(), It.IsAny<CancellationToken>()))
                .Callback<WeeklyAssessment, CancellationToken>((item, _) => AddedWeeklyAssessments.Add(item))
                .Returns(Task.CompletedTask);

            for (int index = 0; index < areas.Length; index++)
            {
                StudyArea area = areas[index];
                StudyPlan plan = StudyPlans[index];
                StudyAreaRepository.Setup(repository => repository.GetByIdAsync(area.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(area);
                StudyPlanRepository.Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(plan);
            }

            Service = new AutoCreateNextWeekService(
                clock,
                UnitOfWork.Object,
                IsoWeekCalendar.Object,
                CurrentWeekProvider.Object,
                StudyAreaRepository.Object,
                StudyPlanRepository.Object,
                StudyAreaWeekRepository.Object,
                WeeklyAssessmentRepository.Object,
                StudyAreaWeekAssessmentRepository.Object);
        }
    }
}
