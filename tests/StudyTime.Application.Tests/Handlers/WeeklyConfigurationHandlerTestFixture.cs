using Moq;
using System.Globalization;
using StudyTime.Domain.Entities;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.Tests.Handlers;

/// <summary>
/// Shared mock setup for weekly-configuration handler unit tests.
/// All dates are fixed, and the current week is supplied by ICurrentWeekProvider.
/// </summary>
internal sealed class WeeklyConfigurationHandlerTestFixture
{
    public static readonly DateOnly CurrentWeekStart = new(2026, 10, 26);
    public DateOnly TargetWeekStart => CurrentWeekStart.AddDays(7);
    public Mock<IStudyAreaRepository> StudyAreaRepository { get; } = new();
    public Mock<IStudyPlanRepository> StudyPlanRepository { get; } = new();
    public Mock<IStudyAreaWeekRepository> StudyAreaWeekRepository { get; } = new();
    public Mock<IWeeklyAssessmentRepository> WeeklyAssessmentRepository { get; } = new();
    public Mock<IStudyAreaWeekAssessmentRepository> StudyAreaWeekAssessmentRepository { get; } = new();
    public Mock<ICurrentWeekProvider> CurrentWeekProvider { get; } = new();
    public Mock<IIsoWeekCalendar> IsoWeekCalendar { get; } = new();
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();
    public Dictionary<Guid, StudyArea> StudyAreasById { get; } = new();
    public Dictionary<Guid, StudyPlan> StudyPlansById { get; } = new();
    public Dictionary<DateOnly, IReadOnlyList<StudyAreaWeek>> StudyAreaWeeksByStart { get; } = new();
    public Dictionary<Guid, StudyAreaWeekAssessment> StudyAreaWeekAssessmentsByWeekId { get; } = new();
    public Dictionary<(int Year, int WeekNumber), WeeklyAssessment> WeeklyAssessmentsByIsoWeek { get; } = new();
    public List<StudyAreaWeek> AddedStudyAreaWeeks { get; } = [];
    public List<StudyAreaWeekAssessment> AddedStudyAreaWeekAssessments { get; } = [];
    public List<WeeklyAssessment> AddedWeeklyAssessments { get; } = [];
    public List<string> CallOrder { get; } = [];
    public IReadOnlyList<StudyAreaWeek> InitialStudyAreaWeeks { get; set; } = Array.Empty<StudyAreaWeek>();
    public StudyArea CurrentStudyArea { get; }
    public StudyPlan CurrentStudyPlan { get; }
    public WeeklyAssessment CurrentWeeklyAssessment { get; }
    public StudyAreaWeek CurrentStudyAreaWeek { get; }
    public StudyAreaWeekAssessment CurrentStudyAreaWeekAssessment { get; }
    public StudyArea TargetStudyArea { get; }
    public StudyPlan TargetStudyPlan { get; }

    public WeeklyConfigurationHandlerTestFixture()
    {
        CurrentStudyArea = new StudyArea("Área corrente", new Minutes(1500));
        CurrentStudyPlan = new StudyPlan("Plano corrente", new Coefficient(1m));
        CurrentWeeklyAssessment = CreateWeeklyAssessment(CurrentWeekStart, 1500m);
        
        CurrentStudyAreaWeek = new StudyAreaWeek(
            CurrentWeekStart,
            CurrentStudyArea.Id,
            CurrentStudyPlan.Id,
            CurrentWeeklyAssessment.Id);

        CurrentStudyAreaWeekAssessment = new StudyAreaWeekAssessment(
            1500m,
            CurrentStudyAreaWeek.Id,
            1500);

        TargetStudyArea = new StudyArea("Área de destino", new Minutes(1500));
        TargetStudyPlan = new StudyPlan("Plano de destino", new Coefficient(1m));

        StudyAreasById.Add(CurrentStudyArea.Id, CurrentStudyArea);
        StudyAreasById.Add(TargetStudyArea.Id, TargetStudyArea);
        StudyPlansById.Add(CurrentStudyPlan.Id, CurrentStudyPlan);
        StudyPlansById.Add(TargetStudyPlan.Id, TargetStudyPlan);

        StudyAreaWeeksByStart[CurrentWeekStart] = new[] { CurrentStudyAreaWeek };
        StudyAreaWeekAssessmentsByWeekId[CurrentStudyAreaWeek.Id] = CurrentStudyAreaWeekAssessment;
        WeeklyAssessmentsByIsoWeek[ToIsoKey(CurrentWeekStart)] = CurrentWeeklyAssessment;

        ConfigureMocks();
    }

    public static IsoWeek GetIsoWeek(DateOnly date)
    {
        DateTime dateTime = date.ToDateTime(TimeOnly.MinValue);
        return new IsoWeek(
            ISOWeek.GetYear(dateTime),
            ISOWeek.GetWeekOfYear(dateTime));
    }

    public static (int Year, int WeekNumber) ToIsoKey(DateOnly date)
    {
        IsoWeek isoWeek = GetIsoWeek(date);
        return (isoWeek.Year, isoWeek.WeekNumber);
    }

    public static WeeklyAssessment CreateWeeklyAssessment(DateOnly weekStartDate, decimal globalGoal)
    {
        IsoWeek isoWeek = GetIsoWeek(weekStartDate);
        return new WeeklyAssessment(isoWeek.WeekNumber, isoWeek.Year, globalGoal);
    }

    public static StudyArea CreateStudyArea(string name, int standardWeeklyMinutes)
    {
        return new StudyArea(name, new Minutes(standardWeeklyMinutes));
    }

    public void AddStudyArea(StudyArea studyArea) => StudyAreasById[studyArea.Id] = studyArea;

    public void SetStudyAreaWeeks(DateOnly weekStartDate, params StudyAreaWeek[] studyAreaWeeks)
    {
        StudyAreaWeeksByStart[weekStartDate] = studyAreaWeeks;
    }

    public void SetStudyAreaWeekAssessment(StudyAreaWeekAssessment assessment)
    {
        StudyAreaWeekAssessmentsByWeekId[assessment.StudyAreaWeekId] = assessment;
    }

    public void SetWeeklyAssessment(DateOnly weekStartDate, WeeklyAssessment assessment)
    {
        IsoWeek isoWeek = GetIsoWeek(weekStartDate);
        WeeklyAssessmentsByIsoWeek[(isoWeek.Year, isoWeek.WeekNumber)] = assessment;
    }

    private void ConfigureMocks()
    {
        CurrentWeekProvider
            .Setup(item => item.GetCurrentWeek())
            .Returns(new WeekRange(CurrentWeekStart));

        // The uploaded source defines the calendar port but does not include its
        // concrete Infrastructure implementation. Use the deterministic BCL ISOWeek.
        IsoWeekCalendar
            .Setup(item => item.GetIsoWeek(It.IsAny<DateOnly>()))
            .Returns((DateOnly date) => GetIsoWeek(date));
        IsoWeekCalendar
            .Setup(item => item.GetMonday(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int year, int weekNumber) => new IsoWeek(year, weekNumber).GetMonday());

        StudyAreaRepository
            .Setup(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                StudyAreasById.TryGetValue(id, out StudyArea? studyArea) ? studyArea : null);

        StudyPlanRepository
            .Setup(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                StudyPlansById.TryGetValue(id, out StudyPlan? studyPlan) ? studyPlan : null);

        StudyAreaWeekRepository
            .Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancellationToken _) => InitialStudyAreaWeeks);

        StudyAreaWeekRepository
            .Setup(item => item.GetByWeekStartDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateOnly weekStartDate, CancellationToken _) =>
                StudyAreaWeeksByStart.TryGetValue(weekStartDate, out IReadOnlyList<StudyAreaWeek>? weeks)
                    ? weeks
                    : (IReadOnlyList<StudyAreaWeek>)Array.Empty<StudyAreaWeek>());

        StudyAreaWeekRepository
            .Setup(item => item.ExistsForStudyAreaInWeekAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid studyAreaId, DateOnly weekStartDate, CancellationToken _) =>
                StudyAreaWeeksByStart.TryGetValue(weekStartDate, out IReadOnlyList<StudyAreaWeek>? weeks) &&
                weeks.Any(item => item.StudyAreaId == studyAreaId));

        StudyAreaWeekRepository
            .Setup(item => item.AddAsync(It.IsAny<StudyAreaWeek>(), It.IsAny<CancellationToken>()))
            .Callback<StudyAreaWeek, CancellationToken>((studyAreaWeek, _) =>
            {
                AddedStudyAreaWeeks.Add(studyAreaWeek);
                CallOrder.Add("StudyAreaWeek.Add");
                IReadOnlyList<StudyAreaWeek> existing =
                    StudyAreaWeeksByStart.TryGetValue(studyAreaWeek.WeekStartDate, out IReadOnlyList<StudyAreaWeek>? weeks)
                        ? weeks
                        : (IReadOnlyList<StudyAreaWeek>)Array.Empty<StudyAreaWeek>();
                StudyAreaWeeksByStart[studyAreaWeek.WeekStartDate] = existing.Append(studyAreaWeek).ToArray();
            })
            .Returns(Task.CompletedTask);

        StudyAreaWeekAssessmentRepository
            .Setup(item => item.GetByStudyAreaWeekIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid studyAreaWeekId, CancellationToken _) =>
                StudyAreaWeekAssessmentsByWeekId.TryGetValue(studyAreaWeekId, out StudyAreaWeekAssessment? assessment)
                    ? assessment
                    : null);

        StudyAreaWeekAssessmentRepository
            .Setup(item => item.AddAsync(It.IsAny<StudyAreaWeekAssessment>(), It.IsAny<CancellationToken>()))
            .Callback<StudyAreaWeekAssessment, CancellationToken>((assessment, _) =>
            {
                AddedStudyAreaWeekAssessments.Add(assessment);
                StudyAreaWeekAssessmentsByWeekId[assessment.StudyAreaWeekId] = assessment;
                CallOrder.Add("StudyAreaWeekAssessment.Add");
            })
            .Returns(Task.CompletedTask);

        WeeklyAssessmentRepository
            .Setup(item => item.GetByYearAndWeekNumberAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((int year, int weekNumber, CancellationToken _) =>
                WeeklyAssessmentsByIsoWeek.TryGetValue((year, weekNumber), out WeeklyAssessment? assessment)
                    ? assessment
                    : null);

        WeeklyAssessmentRepository
            .Setup(item => item.AddAsync(It.IsAny<WeeklyAssessment>(), It.IsAny<CancellationToken>()))
            .Callback<WeeklyAssessment, CancellationToken>((assessment, _) =>
            {
                AddedWeeklyAssessments.Add(assessment);
                WeeklyAssessmentsByIsoWeek[(assessment.Year, assessment.WeekNumber)] = assessment;
                CallOrder.Add("WeeklyAssessment.Add");
            })
            .Returns(Task.CompletedTask);

        WeeklyAssessmentRepository
            .Setup(item => item.Update(It.IsAny<WeeklyAssessment>()))
            .Callback<WeeklyAssessment>(_ => CallOrder.Add("WeeklyAssessment.Update"));

        UnitOfWork
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(_ => CallOrder.Add("UnitOfWork.SaveChanges"))
            .ReturnsAsync(1);
    }
}
