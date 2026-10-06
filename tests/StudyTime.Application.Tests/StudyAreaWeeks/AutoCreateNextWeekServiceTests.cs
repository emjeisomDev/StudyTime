using Moq;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.StudyAreaWeeks.AutoCreation;

namespace StudyTime.Application.Tests.StudyAreaWeeks;

public sealed class AutoCreateNextWeekServiceTests
{
    [Fact]
    public async Task ExecuteAsync_CurrentWeekConfigured_CreatesNextWeekConfiguration()
    {
        var currentWeekStart = new DateOnly(2026, 9, 28);
        var nextWeekStart = currentWeekStart.AddDays(7);

        var studyAreaId = Guid.NewGuid();
        var studyPlanId = Guid.NewGuid();
        var currentWeeklyAssessmentId = Guid.NewGuid();

        var currentStudyAreaWeek = new StudyAreaWeek(
            currentWeekStart,
            studyAreaId,
            studyPlanId,
            currentWeeklyAssessmentId);

        var studyArea = new StudyArea(
            studyAreaId,
            "Matemática",
            new Minutes(1000));

        var studyPlan = new StudyPlan(
            studyPlanId,
            "Plano padrão",
            new Coefficient(1.5m));

        var expectedIndividualGoal =
            GoalCalculator.CalculateIndividualGoal(
                studyArea.StdWeekStudyTime.Value,
                studyPlan.Coefficient.Value);

        var currentAssessment = new StudyAreaWeekAssessment(expectedIndividualGoal, currentStudyAreaWeek.Id);
        var nextIsoWeek = new IsoWeek(2026, 41);
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyAreaRepository = new Mock<IStudyAreaRepository>();
        var studyPlanRepository = new Mock<IStudyPlanRepository>();
        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        ConfigureCurrentWeek(currentWeekProvider, currentWeekStart);

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(
                currentWeekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { currentStudyAreaWeek });

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(
                nextWeekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyAreaWeek>());

        studyAreaRepository
            .Setup(x => x.GetByIdAsync(
                studyAreaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyArea);

        studyPlanRepository
            .Setup(x => x.GetByIdAsync(
                studyPlanId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyPlan);

        studyAreaWeekAssessmentRepository
            .Setup(x => x.GetByStudyAreaWeekIdAsync(
                currentStudyAreaWeek.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentAssessment);

        isoWeekCalendar
            .Setup(x => x.GetIsoWeek(nextWeekStart))
            .Returns(nextIsoWeek);

        weeklyAssessmentRepository
            .Setup(x => x.GetByYearAndWeekNumberAsync(
                nextIsoWeek.Year,
                nextIsoWeek.WeekNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);

        unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService(
            studyAreaRepository,
            studyPlanRepository,
            studyAreaWeekRepository,
            studyAreaWeekAssessmentRepository,
            weeklyAssessmentRepository,
            currentWeekProvider,
            isoWeekCalendar,
            unitOfWork);

        await service.ExecuteAsync();

        weeklyAssessmentRepository.Verify(
            x => x.AddAsync(
                It.Is<WeeklyAssessment>(assessment =>
                    assessment.Year == nextIsoWeek.Year &&
                    assessment.WeekNumber == nextIsoWeek.WeekNumber &&
                    assessment.WeekGlobalGoal == expectedIndividualGoal &&
                    assessment.MinutesStudied == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);

        studyAreaWeekRepository.Verify(
            x => x.AddAsync(
                It.Is<StudyAreaWeek>(week =>
                    week.WeekStartDate == nextWeekStart &&
                    week.StudyAreaId == studyAreaId &&
                    week.StudyPlanId == studyPlanId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        studyAreaWeekAssessmentRepository.Verify(
            x => x.AddAsync(
                It.Is<StudyAreaWeekAssessment>(assessment =>
                    assessment.WeekIndividualGoal == expectedIndividualGoal),
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            x => x.CommitTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            x => x.RollbackTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NextWeekAlreadyConfigured_IsIdempotent()
    {
        var currentWeekStart = new DateOnly(2026, 9, 28);
        var nextWeekStart = currentWeekStart.AddDays(7);

        var existingNextWeek = new StudyAreaWeek(
            nextWeekStart,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        ConfigureCurrentWeek(currentWeekProvider, currentWeekStart);

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(
                nextWeekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingNextWeek });

        var service = CreateService(
            studyAreaWeekRepository: studyAreaWeekRepository,
            currentWeekProvider: currentWeekProvider,
            unitOfWork: unitOfWork);

        await service.ExecuteAsync();

        studyAreaWeekRepository.Verify(
            x => x.AddAsync(
                It.IsAny<StudyAreaWeek>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.CommitTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            x => x.RollbackTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CurrentWeekWithoutConfiguration_DoesNotCreateNextWeek()
    {
        var currentWeekStart = new DateOnly(2026, 9, 28);
        var nextWeekStart = currentWeekStart.AddDays(7);

        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        ConfigureCurrentWeek(currentWeekProvider, currentWeekStart);

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(
                nextWeekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyAreaWeek>());

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(
                currentWeekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyAreaWeek>());

        var service = CreateService(
            studyAreaWeekRepository: studyAreaWeekRepository,
            currentWeekProvider: currentWeekProvider,
            unitOfWork: unitOfWork);

        await service.ExecuteAsync();

        studyAreaWeekRepository.Verify(
            x => x.AddAsync(
                It.IsAny<StudyAreaWeek>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.CommitTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CurrentStudyAreaOrPlanDoesNotExist_RollsBack()
    {
        var currentWeekStart = new DateOnly(2026, 9, 28);
        var nextWeekStart = currentWeekStart.AddDays(7);

        var studyAreaId = Guid.NewGuid();
        var studyPlanId = Guid.NewGuid();

        var currentStudyAreaWeek = new StudyAreaWeek(
            currentWeekStart,
            studyAreaId,
            studyPlanId,
            Guid.NewGuid());

        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaRepository = new Mock<IStudyAreaRepository>();
        var studyPlanRepository = new Mock<IStudyPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        ConfigureCurrentWeek(currentWeekProvider, currentWeekStart);

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(
                nextWeekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyAreaWeek>());

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(
                currentWeekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { currentStudyAreaWeek });

        studyAreaRepository
            .Setup(x => x.GetByIdAsync(
                studyAreaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyArea?)null);

        var service = CreateService(
            studyAreaRepository: studyAreaRepository,
            studyPlanRepository: studyPlanRepository,
            studyAreaWeekRepository: studyAreaWeekRepository,
            currentWeekProvider: currentWeekProvider,
            unitOfWork: unitOfWork);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ExecuteAsync());

        unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.CommitTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.RollbackTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UsesSameWeeklyAssessmentForAllReplicatedConfigurations()
    {
        var currentWeekStart = new DateOnly(2026, 9, 28);
        var nextWeekStart = currentWeekStart.AddDays(7);

        var studyAreaId1 = Guid.NewGuid();
        var studyAreaId2 = Guid.NewGuid();
        var studyPlanId = Guid.NewGuid();

        var currentWeeklyAssessmentId = Guid.NewGuid();

        var currentWeek1 = new StudyAreaWeek(
            currentWeekStart,
            studyAreaId1,
            studyPlanId,
            currentWeeklyAssessmentId);

        var currentWeek2 = new StudyAreaWeek(
            currentWeekStart,
            studyAreaId2,
            studyPlanId,
            currentWeeklyAssessmentId);

        var studyArea1 = new StudyArea(studyAreaId1, "Matemática", new Minutes(1000));
        var studyArea2 = new StudyArea(studyAreaId2, "Português", new Minutes(1000));

        var studyPlan = new StudyPlan(studyPlanId, "Plano 1.5", new Coefficient(1.5m));

        var goal1 = GoalCalculator.CalculateIndividualGoal(
            studyArea1.StdWeekStudyTime.Value,
            studyPlan.Coefficient.Value);

        var goal2 = GoalCalculator.CalculateIndividualGoal(
            studyArea2.StdWeekStudyTime.Value,
            studyPlan.Coefficient.Value);

        var assessment1 = new StudyAreaWeekAssessment(goal1, currentWeek1.Id);
        var assessment2 = new StudyAreaWeekAssessment(goal2, currentWeek2.Id);
        var nextIsoWeek = new IsoWeek(2026, 41);
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyAreaRepository = new Mock<IStudyAreaRepository>();
        var studyPlanRepository = new Mock<IStudyPlanRepository>();
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        ConfigureCurrentWeek(currentWeekProvider, currentWeekStart);

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(
                nextWeekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyAreaWeek>());

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(
                currentWeekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new[]
                {
                    currentWeek1,
                    currentWeek2
                });

        studyAreaRepository
            .Setup(x => x.GetByIdAsync(
                studyAreaId1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyArea1);

        studyAreaRepository
            .Setup(x => x.GetByIdAsync(
                studyAreaId2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyArea2);

        studyPlanRepository
            .Setup(x => x.GetByIdAsync(
                studyPlanId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyPlan);

        studyAreaWeekAssessmentRepository
            .Setup(x => x.GetByStudyAreaWeekIdAsync(
                currentWeek1.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessment1);

        studyAreaWeekAssessmentRepository
            .Setup(x => x.GetByStudyAreaWeekIdAsync(
                currentWeek2.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessment2);

        isoWeekCalendar
            .Setup(x => x.GetIsoWeek(nextWeekStart))
            .Returns(nextIsoWeek);

        weeklyAssessmentRepository
            .Setup(x => x.GetByYearAndWeekNumberAsync(
                nextIsoWeek.Year,
                nextIsoWeek.WeekNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);

        unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var createdWeeklyAssessmentIds = new List<Guid>();
        var createdStudyAreaWeeks = new List<StudyAreaWeek>();

        weeklyAssessmentRepository
            .Setup(x => x.AddAsync(
                It.IsAny<WeeklyAssessment>(),
                It.IsAny<CancellationToken>()))
            .Callback<WeeklyAssessment, CancellationToken>(
                (assessment, _) =>
                    createdWeeklyAssessmentIds.Add(assessment.Id))
            .Returns(Task.CompletedTask);

        studyAreaWeekRepository
            .Setup(x => x.AddAsync(
                It.IsAny<StudyAreaWeek>(),
                It.IsAny<CancellationToken>()))
            .Callback<StudyAreaWeek, CancellationToken>(
                (week, _) =>
                    createdStudyAreaWeeks.Add(week))
            .Returns(Task.CompletedTask);

        var service = CreateService(
            studyAreaRepository,
            studyPlanRepository,
            studyAreaWeekRepository,
            studyAreaWeekAssessmentRepository,
            weeklyAssessmentRepository,
            currentWeekProvider,
            isoWeekCalendar,
            unitOfWork);

        await service.ExecuteAsync();

        var weeklyAssessmentId = Assert.Single(createdWeeklyAssessmentIds);

        Assert.Equal(2, createdStudyAreaWeeks.Count);

        Assert.All(
            createdStudyAreaWeeks,
            week => Assert.Equal(weeklyAssessmentId, week.WeeklyAssessmentId));

        Assert.All(
            createdStudyAreaWeeks,
            week => Assert.Equal(nextWeekStart, week.WeekStartDate));
    }

    private static AutoCreateNextWeekService CreateService(
        Mock<IStudyAreaRepository>? studyAreaRepository = null,
        Mock<IStudyPlanRepository>? studyPlanRepository = null,
        Mock<IStudyAreaWeekRepository>? studyAreaWeekRepository = null,
        Mock<IStudyAreaWeekAssessmentRepository>? studyAreaWeekAssessmentRepository = null,
        Mock<IWeeklyAssessmentRepository>? weeklyAssessmentRepository = null,
        Mock<ICurrentWeekProvider>? currentWeekProvider = null,
        Mock<IIsoWeekCalendar>? isoWeekCalendar = null,
        Mock<IUnitOfWork>? unitOfWork = null)
    {
        return new AutoCreateNextWeekService(
            (studyAreaRepository ?? new Mock<IStudyAreaRepository>()).Object,
            (studyPlanRepository ?? new Mock<IStudyPlanRepository>()).Object,
            (studyAreaWeekRepository ?? new Mock<IStudyAreaWeekRepository>()).Object,
            (studyAreaWeekAssessmentRepository ?? new Mock<IStudyAreaWeekAssessmentRepository>()).Object,
            (weeklyAssessmentRepository ?? new Mock<IWeeklyAssessmentRepository>()).Object,
            (currentWeekProvider ?? new Mock<ICurrentWeekProvider>()).Object,
            (isoWeekCalendar ?? new Mock<IIsoWeekCalendar>()).Object,
            (unitOfWork ?? new Mock<IUnitOfWork>()).Object);
    }

    private static void ConfigureCurrentWeek(
        Mock<ICurrentWeekProvider> currentWeekProvider,
        DateOnly weekStartDate)
    {
        currentWeekProvider
            .Setup(x => x.GetCurrentWeek())
            .Returns(new WeekRange(weekStartDate));
    }
}