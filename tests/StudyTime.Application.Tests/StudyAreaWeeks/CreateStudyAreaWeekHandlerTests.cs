using Moq;
using StudyTime.Application.StudyAreaWeeks.Commands;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Application.Tests.StudyAreaWeeks;

public sealed class CreateStudyAreaWeekHandlerTests
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

    private readonly Guid _studyAreaId = Guid.NewGuid();
    private readonly Guid _studyPlanId = Guid.NewGuid();

    private readonly StudyArea _studyArea;
    private readonly StudyPlan _studyPlan;

    public CreateStudyAreaWeekHandlerTests()
    {
        _studyArea = new StudyArea(
            _studyAreaId,
            "Matemática",
            new Minutes(1500));

        _studyPlan = new StudyPlan(
            _studyPlanId,
            "Plano padrão",
            new Coefficient(1m));

        _currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(CurrentWeekStart));

        _isoWeekCalendar
            .Setup(calendar => calendar.GetIsoWeek(
                It.IsAny<DateOnly>()))
            .Returns((DateOnly date) =>
            {
                var dateTime = date.ToDateTime(TimeOnly.MinValue);
                var year = System.Globalization.ISOWeek.GetYear(dateTime);
                var week = System.Globalization.ISOWeek.GetWeekOfYear(dateTime);

                return new IsoWeek(year, week);
            });

        _weeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);

        _studyAreaWeekRepository
            .Setup(repository => repository.ExistsForStudyAreaInWeekAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _studyAreaWeekRepository
            .Setup(repository => repository.GetByWeekStartDateAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyAreaWeek>());

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                _studyAreaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_studyArea);

        _studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(
                _studyPlanId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_studyPlan);

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
    public async Task Handle_ValidRequest_CreatesStudyAreaWeek()
    {
        var handler = CreateHandler();
        var command = new CreateStudyAreaWeekCommand(
            _studyAreaId,
            _studyPlanId,
            CurrentWeekStart);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(CurrentWeekStart, result.WeekStartDate);
        Assert.Equal(_studyAreaId, result.StudyAreaId);
        Assert.Equal(_studyPlanId, result.StudyPlanId);
        Assert.Equal(1500m, result.WeekIndividualGoal);
        Assert.Equal(0, result.MinutesStudied);
        Assert.False(result.GoalAchieved);

        _weeklyAssessmentRepository.Verify(
            repository => repository.AddAsync(
                It.Is<WeeklyAssessment>(assessment =>
                    assessment.WeekGlobalGoal == 1500m),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _studyAreaWeekRepository.Verify(
            repository => repository.AddAsync(
                It.Is<StudyAreaWeek>(week =>
                    week.StudyAreaId == _studyAreaId &&
                    week.StudyPlanId == _studyPlanId &&
                    week.WeekStartDate == CurrentWeekStart),
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

    [Fact]
    public async Task Handle_StudyAreaNotFound_ThrowsKeyNotFoundException()
    {
        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                _studyAreaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyArea?)null);

        var handler = CreateHandler();
        var command = new CreateStudyAreaWeekCommand(
            _studyAreaId,
            _studyPlanId,
            CurrentWeekStart);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_StudyPlanNotFound_ThrowsKeyNotFoundException()
    {
        _studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(
                _studyPlanId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyPlan?)null);

        var handler = CreateHandler();
        var command = new CreateStudyAreaWeekCommand(
            _studyAreaId,
            _studyPlanId,
            CurrentWeekStart);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_InactiveStudyPlan_ThrowsDomainException()
    {
        _studyPlan.ChangeStatus(StudyPlanStatus.Inactive);

        var handler = CreateHandler();
        var command = new CreateStudyAreaWeekCommand(
            _studyAreaId,
            _studyPlanId,
            CurrentWeekStart);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateStudyAreaInWeek_ThrowsDuplicateException()
    {
        _studyAreaWeekRepository
            .Setup(repository => repository.ExistsForStudyAreaInWeekAsync(
                _studyAreaId,
                CurrentWeekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        var command = new CreateStudyAreaWeekCommand(
            _studyAreaId,
            _studyPlanId,
            CurrentWeekStart);

        await Assert.ThrowsAsync<DuplicateStudyAreaWeekException>(
            () => handler.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_GlobalGoalBelowMinimum_ThrowsWeeklyGoalNotMetException()
    {
        var area = new StudyArea(
            _studyAreaId,
            "Matemática",
            new Minutes(1499));

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                _studyAreaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);

        var handler = CreateHandler();
        var command = new CreateStudyAreaWeekCommand(
            _studyAreaId,
            _studyPlanId,
            CurrentWeekStart);

        await Assert.ThrowsAsync<WeeklyGoalNotMetException>(
            () => handler.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidWeekStartDate_ThrowsDomainException()
    {
        var handler = CreateHandler();
        var command = new CreateStudyAreaWeekCommand(
            _studyAreaId,
            _studyPlanId,
            CurrentWeekStart.AddDays(1));

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WeekOutsideAllowedWindow_ThrowsDomainException()
    {
        var handler = CreateHandler();
        var command = new CreateStudyAreaWeekCommand(
            _studyAreaId,
            _studyPlanId,
            CurrentWeekStart.AddDays(35));

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CurrentWeekGoalNotReached_ThrowsWeekLockedException()
    {
        var isoWeek = _isoWeekCalendar.Object.GetIsoWeek(CurrentWeekStart);
        var currentAssessment = new WeeklyAssessment(
            isoWeek.WeekNumber,
            isoWeek.Year,
            1500m,
            1499);

        _weeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(
                isoWeek.Year,
                isoWeek.WeekNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentAssessment);

        var handler = CreateHandler();
        var command = new CreateStudyAreaWeekCommand(
            _studyAreaId,
            _studyPlanId,
            CurrentWeekStart);

        await Assert.ThrowsAsync<WeekConfigurationLockedException>(
            () => handler.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private CreateStudyAreaWeekHandler CreateHandler()
    {
        return new CreateStudyAreaWeekHandler(
            _studyAreaRepository.Object,
            _studyPlanRepository.Object,
            _studyAreaWeekRepository.Object,
            _studyAreaWeekAssessmentRepository.Object,
            _weeklyAssessmentRepository.Object,
            _currentWeekProvider.Object,
            _isoWeekCalendar.Object,
            _unitOfWork.Object);
    }
}