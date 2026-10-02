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

public sealed class BootstrapSystemHandlerTests
{
    private static readonly DateOnly CurrentWeekStart =
        new(2026, 9, 28);

    private readonly Mock<IStudyAreaRepository> _studyAreaRepository = new();
    private readonly Mock<IStudyPlanRepository> _studyPlanRepository = new();
    private readonly Mock<IStudyAreaWeekRepository> _studyAreaWeekRepository = new();
    private readonly Mock<IStudyAreaWeekAssessmentRepository> _studyAreaWeekAssessmentRepository = new();
    private readonly Mock<IWeeklyAssessmentRepository> _weeklyAssessmentRepository = new();
    private readonly Mock<ICurrentWeekProvider> _currentWeekProvider = new();
    private readonly Mock<IIsoWeekCalendar> _isoWeekCalendar = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private BootstrapSystemHandler CreateSut()
    {
        return new BootstrapSystemHandler(
            _studyAreaRepository.Object,
            _studyPlanRepository.Object,
            _studyAreaWeekRepository.Object,
            _studyAreaWeekAssessmentRepository.Object,
            _weeklyAssessmentRepository.Object,
            _currentWeekProvider.Object,
            _isoWeekCalendar.Object,
            _unitOfWork.Object);
    }

    private void ConfigureCurrentWeek()
    {
        _currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(CurrentWeekStart));

        _isoWeekCalendar
            .Setup(calendar => calendar.GetIsoWeek(CurrentWeekStart))
            .Returns(new IsoWeek(2026, 40));

        _studyAreaWeekRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyAreaWeek>());

        _weeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(
                2026,
                40,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);
    }

    [Fact]
    public async Task Handle_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenItemsAreEmpty_ThrowsArgumentException()
    {
        ConfigureCurrentWeek();
        var sut = CreateSut();

        var command = new BootstrapSystemCommand(
            Array.Empty<StudyAreaWeekBatchItemDto>());

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSystemIsAlreadyInitialized_ThrowsDomainException()
    {
        _studyAreaWeekRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new StudyAreaWeek(
                    CurrentWeekStart,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid())
            });

        var sut = CreateSut();
        var command = new BootstrapSystemCommand(
            new[]
            {
                new StudyAreaWeekBatchItemDto(Guid.NewGuid(), Guid.NewGuid())
            });

        await Assert.ThrowsAsync<DomainException>(
            () => sut.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAreaIsMissing_ThrowsKeyNotFoundException()
    {
        ConfigureCurrentWeek();

        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                areaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyArea?)null);

        var sut = CreateSut();
        var command = new BootstrapSystemCommand(
            new[] { new StudyAreaWeekBatchItemDto(areaId, planId) });

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPlanIsMissing_ThrowsKeyNotFoundException()
    {
        ConfigureCurrentWeek();

        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                areaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StudyArea(
                areaId,
                "Matemática",
                new Minutes(1500)));

        _studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(
                planId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyPlan?)null);

        var sut = CreateSut();
        var command = new BootstrapSystemCommand(
            new[] { new StudyAreaWeekBatchItemDto(areaId, planId) });

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPlanIsInactive_ThrowsDomainException()
    {
        ConfigureCurrentWeek();

        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var plan = new StudyPlan(
            planId,
            "Plano",
            new Coefficient(1m));

        plan.ChangeStatus(StudyPlanStatus.Inactive);

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                areaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StudyArea(
                areaId,
                "Matemática",
                new Minutes(1500)));

        _studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(
                planId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var sut = CreateSut();
        var command = new BootstrapSystemCommand(
            new[] { new StudyAreaWeekBatchItemDto(areaId, planId) });

        await Assert.ThrowsAsync<DomainException>(
            () => sut.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGlobalGoalIsBelowMinimum_ThrowsWeeklyGoalNotMetException()
    {
        ConfigureCurrentWeek();

        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                areaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StudyArea(
                areaId,
                "Matemática",
                new Minutes(1000)));

        _studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(
                planId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StudyPlan(
                planId,
                "Plano",
                new Coefficient(1m)));

        var sut = CreateSut();
        var command = new BootstrapSystemCommand(
            new[] { new StudyAreaWeekBatchItemDto(areaId, planId) });

        await Assert.ThrowsAsync<WeeklyGoalNotMetException>(
            () => sut.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAreaIsRepeated_ThrowsDuplicateStudyAreaWeekException()
    {
        ConfigureCurrentWeek();

        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var sut = CreateSut();
        var command = new BootstrapSystemCommand(
            new[]
            {
                new StudyAreaWeekBatchItemDto(areaId, planId),
                new StudyAreaWeekBatchItemDto(areaId, Guid.NewGuid())
            });

        await Assert.ThrowsAsync<DuplicateStudyAreaWeekException>(
            () => sut.Handle(command, CancellationToken.None));

        _studyAreaRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenConfigurationIsValid_CreatesWeeklyAndIndividualAssessments()
    {
        ConfigureCurrentWeek();

        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                areaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StudyArea(
                areaId,
                "Matemática",
                new Minutes(1500)));

        _studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(
                planId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StudyPlan(
                planId,
                "Plano",
                new Coefficient(1m)));

        var sut = CreateSut();
        var command = new BootstrapSystemCommand(
            new[] { new StudyAreaWeekBatchItemDto(areaId, planId) });

        var result = await sut.Handle(command, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(CurrentWeekStart, dto.WeekStartDate);
        Assert.Equal(areaId, dto.StudyAreaId);
        Assert.Equal(planId, dto.StudyPlanId);
        Assert.Equal(1500m, dto.WeekIndividualGoal);
        Assert.Equal(0, dto.MinutesStudied);
        Assert.False(dto.GoalAchieved);

        _weeklyAssessmentRepository.Verify(
            repository => repository.AddAsync(
                It.Is<WeeklyAssessment>(assessment =>
                    assessment.Year == 2026 &&
                    assessment.WeekNumber == 40 &&
                    assessment.WeekGlobalGoal == 1500m),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _studyAreaWeekRepository.Verify(
            repository => repository.AddAsync(
                It.Is<StudyAreaWeek>(week =>
                    week.WeekStartDate == CurrentWeekStart &&
                    week.StudyAreaId == areaId &&
                    week.StudyPlanId == planId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _studyAreaWeekAssessmentRepository.Verify(
            repository => repository.AddAsync(
                It.Is<StudyAreaWeekAssessment>(assessment =>
                    assessment.WeekIndividualGoal == 1500m &&
                    assessment.MinutesStudied == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}