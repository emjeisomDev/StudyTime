using StudyTime.Application.StudyAreaWeeks;
using StudyTime.Domain.Entities;

namespace Application.Tests.StudyAreaWeeks;

public sealed class UpdateValidationTests
{
    [Fact]
    public async Task UpdateShouldRejectEmptyStudyAreaWeekId()
    {
        var service = TestHelpers.CreateService(new FakeStudyAreaWeekRepository([]));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync(
                Guid.Empty,
                new UpdateStudyAreaWeekRequest(Guid.NewGuid(), null),
                CancellationToken.None));

        Assert.Contains("StudyAreaWeekId", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectNullRequest()
    {
        var service = TestHelpers.CreateService(new FakeStudyAreaWeekRepository([]));

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.UpdateAsync(
                Guid.NewGuid(),
                null!,
                CancellationToken.None));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task UpdateShouldRejectWhenBothFieldsAreOmitted()
    {
        var service = TestHelpers.CreateService(new FakeStudyAreaWeekRepository([]));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync(
                Guid.NewGuid(),
                new UpdateStudyAreaWeekRequest(null, null),
                CancellationToken.None));

        Assert.Contains("At least one", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectEmptyStudyAreaId()
    {
        var service = TestHelpers.CreateService(new FakeStudyAreaWeekRepository([]));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync(
                Guid.NewGuid(),
                new UpdateStudyAreaWeekRequest(Guid.Empty, null),
                CancellationToken.None));

        Assert.Contains("StudyAreaId", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectEmptyStudyPlanId()
    {
        var service = TestHelpers.CreateService(new FakeStudyAreaWeekRepository([]));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync(
                Guid.NewGuid(),
                new UpdateStudyAreaWeekRequest(null, Guid.Empty),
                CancellationToken.None));

        Assert.Contains("StudyPlanId", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldReturnNullWhenStudyAreaWeekDoesNotExist()
    {
        var service = TestHelpers.CreateService(
            new FakeStudyAreaWeekRepository([]),
            new FakeStudyAreaRepository(),
            new FakeStudyPlanRepository(),
            new FixedCalendar(new DateOnly(2026, 8, 31)),
            new FakeUnitOfWork());

        var result = await service.UpdateAsync(
            Guid.NewGuid(),
            new UpdateStudyAreaWeekRequest(Guid.NewGuid(), null),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateShouldRejectStudyAreaWeekOutsideConfigurationWindow()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area,
            plan,
            assessment.Id,
            1500m);

        var service = TestHelpers.CreateService(
            new FakeStudyAreaWeekRepository([studyAreaWeek], assessment),
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(plan),
            new FixedCalendar(week, false),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(area.Id, null),
                CancellationToken.None));

        Assert.Contains(
            "outside the allowed configuration window",
            exception.Message);
    }
}