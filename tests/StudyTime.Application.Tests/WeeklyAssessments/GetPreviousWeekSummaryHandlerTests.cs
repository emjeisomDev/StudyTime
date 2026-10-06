using Moq;
using AutoMapper;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;
using StudyTime.Application.Dtos;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.WeeklyAssessments.Queries;

namespace StudyTime.Application.Tests.WeeklyAssessments;

public sealed class GetPreviousWeekSummaryHandlerTests
{
    [Fact]
    public async Task Handle_returnsPreviousWeekSummary_withGlobalAndIndividualResults()
    {
        var currentWeekStart = new DateOnly(2026, 10, 5);
        var previousWeekStart = currentWeekStart.AddDays(-7);

        var weeklyAssessmentId = Guid.NewGuid();
        var studyAreaWeekId = Guid.NewGuid();
        var studyAreaId = Guid.NewGuid();
        var studyPlanId = Guid.NewGuid();

        var weeklyAssessment = new WeeklyAssessment(weeklyAssessmentId, 40, 2026, 1800m, 1900);

        var studyAreaWeek =
            new StudyAreaWeek(
                studyAreaWeekId,
                previousWeekStart,
                studyAreaId,
                studyPlanId,
                weeklyAssessmentId);

        var individualAssessment = new StudyAreaWeekAssessment(Guid.NewGuid(), 1800m, 1900, studyAreaWeekId);
        var studyArea = new StudyArea(studyAreaId, "Matemática", new Minutes(1200));
        var studyPlan = new StudyPlan(studyPlanId, "Plano Intensivo", new Coefficient(1.5m));
        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyAreaRepository = new Mock<IStudyAreaRepository>();
        var studyPlanRepository = new Mock<IStudyPlanRepository>();
        var mapper = new Mock<IMapper>();

        currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(currentWeekStart));

        isoWeekCalendar
            .Setup(calendar => calendar.GetIsoWeek(previousWeekStart))
            .Returns(new IsoWeek(2026, 40));

        weeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(2026, 40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByWeekStartDateAsync(previousWeekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<StudyAreaWeek>
                {
                    studyAreaWeek
                });

        studyAreaWeekAssessmentRepository
            .Setup(repository => repository.GetByStudyAreaWeekIdAsync(studyAreaWeekId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(individualAssessment);

        studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(studyAreaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyArea);

        studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(studyPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyPlan);

        mapper
            .Setup(value => value.Map<StudyAreaDto>(studyArea))
            .Returns(
                new StudyAreaDto(studyAreaId, "Matemática", 1200));

        mapper
            .Setup(value => value.Map<StudyPlanDto>(studyPlan))
            .Returns(new StudyPlanDto(studyPlanId, "Plano Intensivo", 1.5m, "active"));

        mapper
            .Setup(value =>
                value.Map<WeeklyAssessmentDto>(
                    weeklyAssessment,
                    It.IsAny<Action<IMappingOperationOptions<object, WeeklyAssessmentDto>>>()))
            .Returns(
                new WeeklyAssessmentDto(
                    weeklyAssessment.Id,
                    weeklyAssessment.WeekNumber,
                    weeklyAssessment.Year,
                    weeklyAssessment.WeekGlobalGoal,
                    weeklyAssessment.MinutesStudied,
                    true));

        var handler = new GetPreviousWeekSummaryHandler(
                currentWeekProvider.Object,
                isoWeekCalendar.Object,
                weeklyAssessmentRepository.Object,
                studyAreaWeekRepository.Object,
                studyAreaWeekAssessmentRepository.Object,
                studyAreaRepository.Object,
                studyPlanRepository.Object,
                mapper.Object);

        var result = await handler.Handle(new GetPreviousWeekSummaryQuery(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(previousWeekStart, result.WeekStartDate);
        Assert.Equal(weeklyAssessment.Id, result.WeeklyAssessment.Id);
        Assert.Equal(1800m, result.WeeklyAssessment.WeekGlobalGoal);
        Assert.Equal(1900, result.WeeklyAssessment.MinutesStudied);
        Assert.True(result.WeeklyAssessment.GoalAchieved);

        var item = Assert.Single(result.Items);
        Assert.Equal(studyAreaId, item.StudyArea.Id);
        Assert.Equal(studyPlanId, item.StudyPlan.Id);
        Assert.Equal(1800m, item.WeekIndividualGoal);
        Assert.Equal(1900, item.MinutesStudied);
        Assert.True(item.GoalAchieved);
    }

    [Fact]
    public async Task Handle_returnsGlobalGoalAsNotAchieved_whenIndividualAssessmentIsBelowGoal()
    {
        var currentWeekStart = new DateOnly(2026, 10, 5);
        var previousWeekStart = currentWeekStart.AddDays(-7);
        var weeklyAssessmentId = Guid.NewGuid();
        var studyAreaWeekId = Guid.NewGuid();
        var studyAreaId = Guid.NewGuid();
        var studyPlanId = Guid.NewGuid();

        var weeklyAssessment = new WeeklyAssessment(weeklyAssessmentId, 40, 2026, 1800m, 1700);
        var studyAreaWeek = new StudyAreaWeek(
                studyAreaWeekId,
                previousWeekStart,
                studyAreaId,
                studyPlanId,
                weeklyAssessmentId);

        var individualAssessment = new StudyAreaWeekAssessment(Guid.NewGuid(), 1800m, 1700, studyAreaWeekId);
        var studyArea = new StudyArea(studyAreaId, "Matemática", new Minutes(1200));
        var studyPlan = new StudyPlan(studyPlanId, "Plano Intensivo", new Coefficient(1.5m));
        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyAreaRepository = new Mock<IStudyAreaRepository>();
        var studyPlanRepository = new Mock<IStudyPlanRepository>();
        var mapper = new Mock<IMapper>();

        currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(currentWeekStart));

        isoWeekCalendar
            .Setup(calendar => calendar.GetIsoWeek(previousWeekStart))
            .Returns(new IsoWeek(2026, 40));

        weeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(2026, 40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByWeekStartDateAsync(
                    previousWeekStart,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<StudyAreaWeek>
                {
                    studyAreaWeek
                });

        studyAreaWeekAssessmentRepository
            .Setup(repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(individualAssessment);

        studyAreaRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    studyAreaId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyArea);

        studyPlanRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    studyPlanId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyPlan);

        mapper
            .Setup(value =>
                value.Map<StudyAreaDto>(
                    studyArea))
            .Returns(
                new StudyAreaDto(
                    studyAreaId,
                    "Matemática",
                    1200));

        mapper
            .Setup(value =>
                value.Map<StudyPlanDto>(
                    studyPlan))
            .Returns(
                new StudyPlanDto(
                    studyPlanId,
                    "Plano Intensivo",
                    1.5m,
                    "active"));

        mapper
            .Setup(value =>
                value.Map<WeeklyAssessmentDto>(
                    weeklyAssessment,
                    It.IsAny<Action<IMappingOperationOptions<object, WeeklyAssessmentDto>>>()))
            .Returns(
                new WeeklyAssessmentDto(
                    weeklyAssessment.Id,
                    weeklyAssessment.WeekNumber,
                    weeklyAssessment.Year,
                    weeklyAssessment.WeekGlobalGoal,
                    weeklyAssessment.MinutesStudied,
                    false));

        var handler = new GetPreviousWeekSummaryHandler(
                currentWeekProvider.Object,
                isoWeekCalendar.Object,
                weeklyAssessmentRepository.Object,
                studyAreaWeekRepository.Object,
                studyAreaWeekAssessmentRepository.Object,
                studyAreaRepository.Object,
                studyPlanRepository.Object,
                mapper.Object);

        var result = await handler.Handle(new GetPreviousWeekSummaryQuery(), CancellationToken.None);

        Assert.False(result.WeeklyAssessment.GoalAchieved);
        var item = Assert.Single(result.Items);
        Assert.False(item.GoalAchieved);
        Assert.Equal(1700, item.MinutesStudied);
        Assert.Equal(1800m, item.WeekIndividualGoal);
        Assert.False(GoalAchievedEvaluator.EvaluateIndividual(individualAssessment));
    }

    [Fact]
    public async Task Handle_usesPreviousIsoWeek_whenCurrentWeekCrossesYearBoundary()
    {
        var currentWeekStart = new DateOnly(2027, 1, 4);
        var previousWeekStart = new DateOnly(2026, 12, 28);
        var weeklyAssessmentId = Guid.NewGuid();
        var studyAreaWeekId = Guid.NewGuid();
        var studyAreaId = Guid.NewGuid();
        var studyPlanId = Guid.NewGuid();
        var weeklyAssessment = new WeeklyAssessment(weeklyAssessmentId, 53, 2026, 1500m, 1500);

        var studyAreaWeek =
            new StudyAreaWeek(
                studyAreaWeekId,
                previousWeekStart,
                studyAreaId,
                studyPlanId,
                weeklyAssessmentId);

        var individualAssessment =
            new StudyAreaWeekAssessment(
                Guid.NewGuid(),
                1500m,
                1500,
                studyAreaWeekId);

        var studyArea =
            new StudyArea(
                studyAreaId,
                "Programação",
                new Minutes(1500));

        var studyPlan =
            new StudyPlan(
                studyPlanId,
                "Plano Padrão",
                new Coefficient(1m));

        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyAreaRepository = new Mock<IStudyAreaRepository>();
        var studyPlanRepository = new Mock<IStudyPlanRepository>();
        var mapper = new Mock<IMapper>();

        currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(currentWeekStart));

        isoWeekCalendar
            .Setup(calendar =>
                calendar.GetIsoWeek(previousWeekStart))
            .Returns(
                new IsoWeek(2026, 53));

        weeklyAssessmentRepository
            .Setup(repository =>
                repository.GetByYearAndWeekNumberAsync(
                    2026,
                    53,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByWeekStartDateAsync(
                    previousWeekStart,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<StudyAreaWeek>
                {
                    studyAreaWeek
                });

        studyAreaWeekAssessmentRepository
            .Setup(repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(individualAssessment);

        studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(studyAreaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyArea);

        studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(studyPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyPlan);

        mapper
            .Setup(value => value.Map<StudyAreaDto>(studyArea))
            .Returns(new StudyAreaDto(studyAreaId, "Programação", 1500));

        mapper
            .Setup(value => value.Map<StudyPlanDto>(studyPlan))
            .Returns(new StudyPlanDto(studyPlanId, "Plano Padrão", 1m, "active"));

        mapper
            .Setup(value =>
                value.Map<WeeklyAssessmentDto>(
                    weeklyAssessment,
                    It.IsAny<Action<IMappingOperationOptions<object, WeeklyAssessmentDto>>>()))
            .Returns(
                new WeeklyAssessmentDto(
                    weeklyAssessment.Id,
                    weeklyAssessment.WeekNumber,
                    weeklyAssessment.Year,
                    weeklyAssessment.WeekGlobalGoal,
                    weeklyAssessment.MinutesStudied,
                    true));

        var handler =
            new GetPreviousWeekSummaryHandler(
                currentWeekProvider.Object,
                isoWeekCalendar.Object,
                weeklyAssessmentRepository.Object,
                studyAreaWeekRepository.Object,
                studyAreaWeekAssessmentRepository.Object,
                studyAreaRepository.Object,
                studyPlanRepository.Object,
                mapper.Object);

        var result = await handler.Handle(new GetPreviousWeekSummaryQuery(), CancellationToken.None);

        Assert.Equal(previousWeekStart, result.WeekStartDate);
        Assert.Equal(2026, result.WeeklyAssessment.Year);
        Assert.Equal(53, result.WeeklyAssessment.WeekNumber);

        weeklyAssessmentRepository.Verify(
            repository => repository.GetByYearAndWeekNumberAsync(2026, 53, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}