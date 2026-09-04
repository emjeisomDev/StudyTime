using StudyTime.Application.Common.Calendar;
using StudyTime.Application.Common.Transactions;
using StudyTime.Application.StudyAreas;
using StudyTime.Application.StudyPlans;
using StudyTime.Application.StudyAreaWeeks;
using StudyTime.Domain.Entities;

namespace Application.Tests.StudyAreaWeeks;

internal static class TestHelpers
{
    public static StudyAreaWeekService CreateService(
        FakeStudyAreaWeekRepository repository,
        FakeStudyAreaRepository? studyAreaRepository = null,
        FakeStudyPlanRepository? studyPlanRepository = null,
        IApplicationCalendar? calendar = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new StudyAreaWeekService(
            repository,
            studyAreaRepository ?? new FakeStudyAreaRepository(),
            studyPlanRepository ?? new FakeStudyPlanRepository(),
            calendar ?? new FixedCalendar(new DateOnly(2026, 8, 31)),
            unitOfWork ?? new FakeUnitOfWork());
    }

    public static StudyRecord CreateStudyRecord(DateOnly week, Guid studyAreaWeekId, int minutes)
    {
        return StudyRecord.Create(
            Guid.NewGuid(),
            week.AddDays(1),
            DateTimeOffset.UtcNow,
            minutes,
            studyAreaWeekId,
            week);
    }

    public static WeeklyAssessment CreateAchievedCurrentWeekAssessment(DateOnly currentWeek, int minutes = 1500)
    {
        var date = currentWeek.ToDateTime(TimeOnly.MinValue);
        var year = System.Globalization.ISOWeek.GetYear(date);
        var weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(date);

        return WeeklyAssessment.Create(year, weekNumber, minutes);
    }

    public static WeeklyAssessment CreateAchievedCurrentWeekAssessment(DateOnly currentWeek, Guid studyAreaWeekId, int minutes = 1500)
    {
        return CreateAchievedCurrentWeekAssessment(currentWeek, minutes);
    }
}

internal sealed class FakeStudyAreaWeekRepository : IStudyAreaWeekRepository
{
    private readonly List<StudyAreaWeek> _studyAreaWeeks;
    private readonly Dictionary<(int Year, int WeekNumber), WeeklyAssessment> _weeklyAssessments = [];

    public List<StudyRecord> StudyRecords { get; } = [];

    public FakeStudyAreaWeekRepository(
        IReadOnlyList<StudyAreaWeek> studyAreaWeeks,
        WeeklyAssessment? weeklyAssessment = null,
        IEnumerable<StudyRecord>? studyRecords = null)
    {
        _studyAreaWeeks = studyAreaWeeks.ToList();

        if (weeklyAssessment is not null)
        {
            _weeklyAssessments[(weeklyAssessment.Year, weeklyAssessment.WeekNumber)] = weeklyAssessment;
        }

        if (studyRecords is not null)
        {
            StudyRecords.AddRange(studyRecords);
        }
    }

    public Task<StudyAreaWeek?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
        => Task.FromResult(
            _studyAreaWeeks.SingleOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<StudyAreaWeek>> ListByWeekAsync(
        DateOnly weekStartDate,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<StudyAreaWeek> result = _studyAreaWeeks
            .Where(x => x.WeekStartDate == weekStartDate)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<bool> ExistsByAreaAndWeekAsync(
        Guid studyAreaId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken)
        => Task.FromResult(
            _studyAreaWeeks.Any(x =>
                x.StudyAreaId == studyAreaId &&
                x.WeekStartDate == weekStartDate));

    public Task<WeeklyAssessment?> GetWeeklyAssessmentAsync(
        int year,
        int weekNumber,
        CancellationToken cancellationToken)
        => Task.FromResult(
            _weeklyAssessments.TryGetValue(
                (year, weekNumber),
                out var assessment)
                ? assessment
                : null);

    public Task<IReadOnlyList<StudyRecord>> ListStudyRecordsByWeekAsync(
        DateOnly weekStartDate,
        CancellationToken cancellationToken)
    {
        var weekIds = _studyAreaWeeks
            .Where(x => x.WeekStartDate == weekStartDate)
            .Select(x => x.Id)
            .ToHashSet();

        IReadOnlyList<StudyRecord> result = StudyRecords
            .Where(x => weekIds.Contains(x.StudyAreaWeekId))
            .ToList();

        return Task.FromResult(result);
    }

    public void Add(StudyAreaWeek studyAreaWeek)
        => _studyAreaWeeks.Add(studyAreaWeek);

    public void AddWeeklyAssessment(WeeklyAssessment weeklyAssessment)
        => _weeklyAssessments[
            (weeklyAssessment.Year, weeklyAssessment.WeekNumber)] = weeklyAssessment;

    public void SetWeeklyAssessment(
        DateOnly weekStartDate,
        WeeklyAssessment? assessment)
    {
        var date = weekStartDate.ToDateTime(TimeOnly.MinValue);

        var key = (
            System.Globalization.ISOWeek.GetYear(date),
            System.Globalization.ISOWeek.GetWeekOfYear(date));

        if (assessment is null)
        {
            _weeklyAssessments.Remove(key);
        }
        else
        {
            _weeklyAssessments[key] = assessment;
        }
    }
}

internal sealed class FakeStudyAreaRepository(
    params StudyArea[] studyAreas) : IStudyAreaRepository
{
    private readonly List<StudyArea> _studyAreas = studyAreas.ToList();

    public Task<StudyArea?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
        => Task.FromResult(
            _studyAreas.SingleOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<StudyArea>> ListAsync(
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<StudyArea>>(_studyAreas);

    public Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludedId,
        CancellationToken cancellationToken)
        => Task.FromResult(
            _studyAreas.Any(x =>
                x.Name == name &&
                x.Id != excludedId));

    public Task<bool> HasDependenciesAsync(
        Guid studyAreaId,
        CancellationToken cancellationToken)
        => Task.FromResult(false);

    public void Add(StudyArea studyArea)
        => _studyAreas.Add(studyArea);

    public void Remove(StudyArea studyArea)
        => _studyAreas.Remove(studyArea);

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}

internal sealed class FakeStudyPlanRepository(
    params StudyPlan[] studyPlans) : IStudyPlanRepository
{
    private readonly List<StudyPlan> _studyPlans = studyPlans.ToList();

    public Task<StudyPlan?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
        => Task.FromResult(
            _studyPlans.SingleOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<StudyPlan>> ListAsync(
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<StudyPlan>>(_studyPlans);

    public void Add(StudyPlan studyPlan)
        => _studyPlans.Add(studyPlan);

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}

internal sealed class FixedCalendar(
    DateOnly currentWeekStartDate,
    bool withinWindow = true) : IApplicationCalendar
{
    public ApplicationWeek CurrentWeek
        => new(currentWeekStartDate);

    public ApplicationWeek PreviousWeek
        => CurrentWeek.AddWeeks(-1);

    public ApplicationWeek NextWeek
        => CurrentWeek.AddWeeks(1);

    public IReadOnlyList<ApplicationWeek> ConfigurationWeeks
        => [CurrentWeek, NextWeek];

    public ApplicationWeek GetWeek(DateOnly dateWeek)
    {
        var daysSinceMonday = ((int)dateWeek.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

        return new ApplicationWeek(dateWeek.AddDays(-daysSinceMonday));
    }

    public bool IsWithinConfigurationWindow(
        DateOnly weekStartDate)
        => withinWindow;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int BeginTransactionCalls { get; private set; }

    public int SaveChangesCalls { get; private set; }

    public Exception? SaveChangesException { get; init; }

    public FakeTransaction Transaction { get; } = new();

    public Task<ITransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        BeginTransactionCalls++;

        return Task.FromResult<ITransaction>(
            Transaction);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;

        if (SaveChangesException is not null)
        {
            throw SaveChangesException;
        }

        return Task.FromResult(1);
    }
}

internal sealed class FakeTransaction : ITransaction
{
    public int CommitCalls { get; private set; }

    public int RollbackCalls { get; private set; }

    public bool IsCommitted { get; private set; }

    public Exception? CommitException { get; set; }

    public Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        CommitCalls++;

        if (CommitException is not null)
        {
            throw CommitException;
        }

        IsCommitted = true;

        return Task.CompletedTask;
    }

    public Task RollbackAsync(
        CancellationToken cancellationToken = default)
    {
        RollbackCalls++;
        IsCommitted = false;

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}