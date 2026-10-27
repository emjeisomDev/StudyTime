using Moq;
using Xunit;
using AutoMapper;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.WeeklyAssessments.Queries;

namespace StudyTime.Application.Tests.RolloverAndSummary;

public sealed class GetPreviousWeekSummaryHandlerTests
{
    private const string IndividualAssessmentsContextKey = "IndividualAssessments";
    private static readonly DateOnly CurrentWeekStart = new(2026, 10, 26);
    private static readonly DateOnly PreviousWeekStart = new(2026, 10, 19);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_DuasConfiguracoes_RetornaMetaGlobalDerivadaDasMetasIndividuais(bool bothGoalsAchieved)
    {
        // Arrange
        TestContext context = new(bothGoalsAchieved);

        // Act
        PreviousWeekSummaryDto result = await context.Handler.Handle(
            new GetPreviousWeekSummaryQuery(),
            CancellationToken.None);

        // Assert
        Assert.Equal(PreviousWeekStart, result.WeekStartDate);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items[0].GoalAchieved);
        Assert.Equal(bothGoalsAchieved, result.Items[1].GoalAchieved);
        Assert.Equal(bothGoalsAchieved, result.WeeklyAssessment.GoalAchieved);
        Assert.Equal(1500m, result.WeeklyAssessment.WeekGlobalGoal);
        Assert.Equal(context.WeeklyAssessment.Id, result.WeeklyAssessment.Id);
        Assert.Equal(2026, result.WeeklyAssessment.Year);
        Assert.Equal(43, result.WeeklyAssessment.WeekNumber);

        context.WeeklyAssessmentRepository.Verify(
            repository => repository.GetByYearAndWeekNumberAsync(2026, 43, It.IsAny<CancellationToken>()),
            Times.Once);
        context.StudyAreaWeekRepository.Verify(
            repository => repository.GetByWeekStartDateAsync(PreviousWeekStart, It.IsAny<CancellationToken>()),
            Times.Once);
        context.Mapper.Verify(
            mapper => mapper.Map<WeeklyAssessmentDto>(
                It.IsAny<object>(),
                It.IsAny<Action<IMappingOperationOptions<object, WeeklyAssessmentDto>>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AvaliacaoSemanalAnteriorAusente_LancaKeyNotFoundException()
    {
        // Arrange
        TestContext context = new(false);
        context.WeeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(2026, 43, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);

        // Act
        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => context.Handler.Handle(new GetPreviousWeekSummaryQuery(), CancellationToken.None));

        // Assert
        Assert.Contains("WeeklyAssessment for ISO week 2026-W43 was not found", exception.Message, StringComparison.Ordinal);
        context.StudyAreaWeekRepository.Verify(
            repository => repository.GetByWeekStartDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AvaliacaoSemanalDeOutraSemana_LancaDomainException()
    {
        // Arrange
        TestContext context = new(false);
        WeeklyAssessment mismatchedAssessment = new(Guid.NewGuid(), 42, 2026, 1500m);
        context.WeeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(2026, 43, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mismatchedAssessment);

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => context.Handler.Handle(new GetPreviousWeekSummaryQuery(), CancellationToken.None));

        // Assert
        Assert.Contains("does not match the previous ISO week", exception.Message, StringComparison.Ordinal);
        context.StudyAreaWeekRepository.Verify(
            repository => repository.GetByWeekStartDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class TestContext
    {
        public Mock<IMapper> Mapper { get; } = new();
        public Mock<IIsoWeekCalendar> IsoWeekCalendar { get; } = new();
        public Mock<ICurrentWeekProvider> CurrentWeekProvider { get; } = new();
        public Mock<IStudyAreaRepository> StudyAreaRepository { get; } = new();
        public Mock<IStudyPlanRepository> StudyPlanRepository { get; } = new();
        public Mock<IStudyAreaWeekRepository> StudyAreaWeekRepository { get; } = new();
        public Mock<IWeeklyAssessmentRepository> WeeklyAssessmentRepository { get; } = new();
        public Mock<IStudyAreaWeekAssessmentRepository> StudyAreaWeekAssessmentRepository { get; } = new();
        public WeeklyAssessment WeeklyAssessment { get; }
        public GetPreviousWeekSummaryHandler Handler { get; }

        public TestContext(bool bothGoalsAchieved)
        {
            CurrentWeekProvider.Setup(provider => provider.GetCurrentWeek())
                .Returns(new WeekRange(CurrentWeekStart));
            IsoWeekCalendar.Setup(calendar => calendar.GetIsoWeek(PreviousWeekStart))
                .Returns(new IsoWeek(2026, 43));

            WeeklyAssessment = new WeeklyAssessment(
                Guid.NewGuid(),
                43,
                2026,
                1500m,
                bothGoalsAchieved ? 1500 : 1250);

            StudyArea areaOne = new(Guid.NewGuid(), "Matemática", new Minutes(750));
            StudyArea areaTwo = new(Guid.NewGuid(), "Física", new Minutes(750));
            StudyPlan planOne = new(Guid.NewGuid(), "Plano A", new Coefficient(1m));
            StudyPlan planTwo = new(Guid.NewGuid(), "Plano B", new Coefficient(1m));

            StudyAreaWeek studyAreaWeekOne = new(
                Guid.NewGuid(),
                PreviousWeekStart,
                areaOne.Id,
                planOne.Id,
                WeeklyAssessment.Id);
            StudyAreaWeek studyAreaWeekTwo = new(
                Guid.NewGuid(),
                PreviousWeekStart,
                areaTwo.Id,
                planTwo.Id,
                WeeklyAssessment.Id);

            StudyAreaWeekAssessment individualAssessmentOne = new(
                Guid.NewGuid(), 750m, 750, studyAreaWeekOne.Id);
            StudyAreaWeekAssessment individualAssessmentTwo = new(
                Guid.NewGuid(), 750m, bothGoalsAchieved ? 750 : 500, studyAreaWeekTwo.Id);

            WeeklyAssessmentRepository
                .Setup(repository => repository.GetByYearAndWeekNumberAsync(2026, 43, It.IsAny<CancellationToken>()))
                .ReturnsAsync(WeeklyAssessment);
            StudyAreaWeekRepository
                .Setup(repository => repository.GetByWeekStartDateAsync(PreviousWeekStart, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<StudyAreaWeek>)new[] { studyAreaWeekOne, studyAreaWeekTwo });

            StudyAreaRepository.Setup(repository => repository.GetByIdAsync(areaOne.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(areaOne);
            StudyAreaRepository.Setup(repository => repository.GetByIdAsync(areaTwo.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(areaTwo);
            StudyPlanRepository.Setup(repository => repository.GetByIdAsync(planOne.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(planOne);
            StudyPlanRepository.Setup(repository => repository.GetByIdAsync(planTwo.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(planTwo);

            StudyAreaWeekAssessmentRepository
                .Setup(repository => repository.GetByStudyAreaWeekIdAsync(studyAreaWeekOne.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(individualAssessmentOne);
            StudyAreaWeekAssessmentRepository
                .Setup(repository => repository.GetByStudyAreaWeekIdAsync(studyAreaWeekTwo.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(individualAssessmentTwo);

            Mapper.Setup(mapper => mapper.Map<StudyAreaDto>(It.IsAny<object>()))
                .Returns((object source) =>
                {
                    StudyArea area = Assert.IsType<StudyArea>(source);
                    return new StudyAreaDto(area.Id, area.Name, area.StdWeekStudyTime.Value);
                });
            Mapper.Setup(mapper => mapper.Map<StudyPlanDto>(It.IsAny<object>()))
                .Returns((object source) =>
                {
                    StudyPlan plan = Assert.IsType<StudyPlan>(source);
                    return new StudyPlanDto(
                        plan.Id,
                        plan.Name,
                        plan.Coefficient.Value,
                        plan.Status.ToString().ToLowerInvariant());
                });
            Mapper.Setup(mapper => mapper.Map<WeeklyAssessmentDto>(
                    It.IsAny<object>(),
                    It.IsAny<Action<IMappingOperationOptions<object, WeeklyAssessmentDto>>>()))
                .Returns((object source, Action<IMappingOperationOptions<object, WeeklyAssessmentDto>> optionsAction) =>
                {
                    WeeklyAssessment weeklyAssessment = Assert.IsType<WeeklyAssessment>(source);
                    Dictionary<string, object> contextItems = new();
                    Mock<IMappingOperationOptions<object, WeeklyAssessmentDto>> options = new();
                    options.SetupGet(option => option.Items).Returns(contextItems);
                    optionsAction(options.Object);

                    IEnumerable<StudyAreaWeekAssessment> assessments =
                        Assert.IsAssignableFrom<IEnumerable<StudyAreaWeekAssessment>>(
                            contextItems[IndividualAssessmentsContextKey]);

                    return new WeeklyAssessmentDto(
                        weeklyAssessment.Id,
                        weeklyAssessment.WeekNumber,
                        weeklyAssessment.Year,
                        weeklyAssessment.WeekGlobalGoal,
                        weeklyAssessment.MinutesStudied,
                        GoalAchievedEvaluator.EvaluateGlobal(assessments));
                });

            Handler = new GetPreviousWeekSummaryHandler(
                Mapper.Object,
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
