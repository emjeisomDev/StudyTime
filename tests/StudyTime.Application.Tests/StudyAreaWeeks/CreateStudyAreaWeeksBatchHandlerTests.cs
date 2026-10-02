using Moq;
using StudyTime.Application.Dtos;
using StudyTime.Application.StudyAreaWeeks.Commands;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Exceptions;

using StudyTime.Domain.ValueObjects;


namespace StudyTime.Application.Tests.StudyAreaWeeks;

public sealed class CreateStudyAreaWeeksBatchHandlerTests
{
    private static readonly DateOnly CurrentWeekStart =
        new(2026, 9, 28);

    private readonly Mock<IStudyAreaRepository> _studyAreaRepository = new();
    private readonly Mock<IStudyPlanRepository> _studyPlanRepository = new();
    private readonly Mock<IStudyAreaWeekRepository> _studyAreaWeekRepository = new();
    private readonly Mock<IStudyAreaWeekAssessmentRepository>
        _studyAreaWeekAssessmentRepository = new();
    private readonly Mock<IWeeklyAssessmentRepository>
        _weeklyAssessmentRepository = new();
    private readonly Mock<ICurrentWeekProvider> _currentWeekProvider = new();
    private readonly Mock<IIsoWeekCalendar> _isoWeekCalendar = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly StudyArea _area1;
    private readonly StudyArea _area2;
    private readonly StudyPlan _plan;

    public CreateStudyAreaWeeksBatchHandlerTests()
    {
        _area1 = new StudyArea(
            Guid.NewGuid(),
            "Matemática",
            new Minutes(900));

        _area2 = new StudyArea(
            Guid.NewGuid(),
            "Programação",
            new Minutes(900));

        _plan = new StudyPlan(
            Guid.NewGuid(),
            "Padrão",
            new Coefficient(1m));

        _currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(CurrentWeekStart));

        _isoWeekCalendar
            .Setup(calendar => calendar.GetIsoWeek(CurrentWeekStart))
            .Returns(new IsoWeek(2026, 40));

        _isoWeekCalendar
            .Setup(calendar => calendar.GetIsoWeek(new DateOnly(2026, 10, 5)))
            .Returns(new IsoWeek(2026, 41));

        _studyAreaWeekRepository
            .Setup(repository => repository.GetByWeekStartDateAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyAreaWeek>());

        _studyAreaWeekRepository
            .Setup(repository => repository.ExistsForStudyAreaInWeekAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var currentWeeklyAssessment = new WeeklyAssessment(
            Guid.NewGuid(),
            40,
            2026,
            1800m,
            1800);

        _weeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(
                2026,
                40,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentWeeklyAssessment);

        _weeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(
                2026,
                41,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                _area1.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_area1);

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                _area2.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_area2);

        _studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(
                _plan.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_plan);

        _weeklyAssessmentRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<WeeklyAssessment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _studyAreaWeekRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<StudyAreaWeek>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _studyAreaWeekAssessmentRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<StudyAreaWeekAssessment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_ConfiguracaoValida_CriaTodasAsAreasDaSemana()
    {
        var handler = CreateHandler();
        var command = new CreateStudyAreaWeeksBatchCommand(
            new DateOnly(2026, 10, 5),
            new[]
            {
                new StudyAreaWeekBatchItemDto(_area1.Id, _plan.Id),
                new StudyAreaWeekBatchItemDto(_area2.Id, _plan.Id)
            });

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.NotNull(_createdWeeklyAssessment);

        Assert.All(result, item =>
        {
            Assert.Equal(new DateOnly(2026, 10, 5), item.WeekStartDate);
            Assert.Equal(_createdWeeklyAssessment!.Id, item.WeeklyAssessmentId);
            Assert.False(item.GoalAchieved);
        });

        Assert.Equal(1800m, result.Sum(item => item.WeekIndividualGoal));

        _weeklyAssessmentRepository.Verify(
            repository => repository.AddAsync(
                It.Is<WeeklyAssessment>(assessment =>
                    assessment.Year == 2026 &&
                    assessment.WeekNumber == 41 &&
                    assessment.WeekGlobalGoal == 1800m),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _studyAreaWeekRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<StudyAreaWeek>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _studyAreaWeekAssessmentRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<StudyAreaWeekAssessment>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AreaDuplicadaNoLote_LancaExcecao()
    {
        var handler = CreateHandler();
        var command = new CreateStudyAreaWeeksBatchCommand(
            CurrentWeekStart,
            new[]
            {
                new StudyAreaWeekBatchItemDto(_area1.Id, _plan.Id),
                new StudyAreaWeekBatchItemDto(_area1.Id, _plan.Id)
            });

        await Assert.ThrowsAsync<DuplicateStudyAreaWeekException>(
            () => handler.Handle(command, CancellationToken.None));

        VerifyNothingWasPersisted();
    }

    [Fact]
    public async Task Handle_AreaInexistente_LancaExcecao()
    {
        var unknownAreaId = Guid.NewGuid();

        var handler = CreateHandler();
        var command = new CreateStudyAreaWeeksBatchCommand(
            CurrentWeekStart,
            new[]
            {
                new StudyAreaWeekBatchItemDto(unknownAreaId, _plan.Id),
                new StudyAreaWeekBatchItemDto(_area1.Id, _plan.Id)
            });

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        VerifyNothingWasPersisted();
    }

    [Fact]
    public async Task Handle_PlanoInativo_LancaExcecao()
    {
        _plan.ChangeStatus(StudyPlanStatus.Inactive);

        var handler = CreateHandler();
        var command = new CreateStudyAreaWeeksBatchCommand(
            CurrentWeekStart,
            new[]
            {
                new StudyAreaWeekBatchItemDto(_area1.Id, _plan.Id),
                new StudyAreaWeekBatchItemDto(_area2.Id, _plan.Id)
            });

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        VerifyNothingWasPersisted();
    }

    [Fact]
    public async Task Handle_MetaGlobalInferiorAMinima_LancaExcecao()
    {
        var lowGoalArea = new StudyArea(
            Guid.NewGuid(),
            "Área com meta baixa",
            new Minutes(500));

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                lowGoalArea.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lowGoalArea);

        var handler = CreateHandler();
        var command = new CreateStudyAreaWeeksBatchCommand(
            CurrentWeekStart,
            new[]
            {
                new StudyAreaWeekBatchItemDto(lowGoalArea.Id, _plan.Id)
            });

        await Assert.ThrowsAsync<WeeklyGoalNotMetException>(
            () => handler.Handle(command, CancellationToken.None));

        VerifyNothingWasPersisted();
    }

    private WeeklyAssessment? _createdWeeklyAssessment;

    private CreateStudyAreaWeeksBatchHandler CreateHandler()
    {
        _weeklyAssessmentRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<WeeklyAssessment>(),
                It.IsAny<CancellationToken>()))
            .Callback<WeeklyAssessment, CancellationToken>(
                (assessment, _) => _createdWeeklyAssessment = assessment)
            .Returns(Task.CompletedTask);

        return new CreateStudyAreaWeeksBatchHandler(
            _studyAreaRepository.Object,
            _studyPlanRepository.Object,
            _studyAreaWeekRepository.Object,
            _studyAreaWeekAssessmentRepository.Object,
            _weeklyAssessmentRepository.Object,
            _currentWeekProvider.Object,
            _isoWeekCalendar.Object,
            _unitOfWork.Object);
    }

    private void VerifyNothingWasPersisted()
    {
        _weeklyAssessmentRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<WeeklyAssessment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _studyAreaWeekRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<StudyAreaWeek>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _studyAreaWeekAssessmentRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<StudyAreaWeekAssessment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}