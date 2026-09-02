using StudyTime.Application.StudyAreaWeeks;
using StudyTime.Domain.Entities;

namespace Application.Tests.StudyAreaWeeks;

public sealed class StudyAreaWeekAssessmentServiceTests
{
    [Fact]
    public async Task GetAssessmentShouldReturnIndividualAssessment()
    {
        var studyArea = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var studyPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessmentId = Guid.NewGuid();
        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            new DateOnly(2026, 9, 7),
            studyArea,
            studyPlan,
            weeklyAssessmentId,
            1000m);

        studyAreaWeek.Assessment.UpdateMinutesStudied(1000);

        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek);
        var service = new StudyAreaWeekService(repository, null!, null!, null!, null!);

        var result = await service.GetAssessmentAsync(studyAreaWeek.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(studyAreaWeek.Id, result.StudyAreaWeekId);
        Assert.Equal(1000m, result.WeekIndividualGoal);
        Assert.Equal(1000, result.MinutesStudied);
        Assert.True(result.GoalAchieved);
    }

    [Fact]
    public async Task GetAssessmentShouldReturnFalseWhenIndividualGoalIsNotAchieved()
    {
        var studyArea = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var studyPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            new DateOnly(2026, 9, 7),
            studyArea,
            studyPlan,
            Guid.NewGuid(),
            1000m);

        studyAreaWeek.Assessment.UpdateMinutesStudied(999);

        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek);
        var service = new StudyAreaWeekService(repository, null!, null!, null!, null!);

        var result = await service.GetAssessmentAsync(studyAreaWeek.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1000m, result.WeekIndividualGoal);
        Assert.Equal(999, result.MinutesStudied);
        Assert.False(result.GoalAchieved);
    }

    [Fact]
    public async Task GetAssessmentShouldReturnNullWhenStudyAreaWeekDoesNotExist()
    {
        var repository = new FakeStudyAreaWeekRepository(null);
        var service = new StudyAreaWeekService(repository, null!, null!, null!, null!);
        var studyAreaWeekId = Guid.NewGuid();

        var result = await service.GetAssessmentAsync(studyAreaWeekId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssessmentShouldRejectEmptyStudyAreaWeekId()
    {
        var repository = new FakeStudyAreaWeekRepository(null);
        var service = new StudyAreaWeekService(repository, null!, null!, null!, null!);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetAssessmentAsync(Guid.Empty, CancellationToken.None));

        Assert.Contains("StudyAreaWeekId", exception.Message);
    }

    private sealed class FakeStudyAreaWeekRepository(StudyAreaWeek? studyAreaWeek) : IStudyAreaWeekRepository
    {
        private readonly StudyAreaWeek? _studyAreaWeek = studyAreaWeek;

        public Task<StudyAreaWeek?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var result = _studyAreaWeek is not null && _studyAreaWeek.Id == id ? _studyAreaWeek : null;
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<StudyAreaWeek>> ListByWeekAsync(
            DateOnly weekStartDate,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<StudyAreaWeek> result = Array.Empty<StudyAreaWeek>();
            return Task.FromResult(result);
        }

        public Task<bool> ExistsByAreaAndWeekAsync(
            Guid studyAreaId,
            DateOnly weekStartDate,
            CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<WeeklyAssessment?> GetWeeklyAssessmentAsync(
            int year,
            int weekNumber,
            CancellationToken cancellationToken)
            => Task.FromResult<WeeklyAssessment?>(null);

        public void Add(StudyAreaWeek studyAreaWeek)
        {
            throw new NotSupportedException("This test repository does not support adding StudyAreaWeek.");
        }

        public void AddWeeklyAssessment(WeeklyAssessment weeklyAssessment)
        {
            throw new NotSupportedException("This test repository does not support adding WeeklyAssessment.");
        }
    }
}