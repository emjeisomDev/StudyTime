using StudyTime.Domain.Services;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.WeeklyAssessments.Services;

public sealed class WeeklyAssessmentSynchronizer(
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository,
    IStudyRecordRepository studyRecordRepository,
    IIsoWeekCalendar isoWeekCalendar,
    IUnitOfWork unitOfWork)
    : IWeeklyAssessmentSynchronizer
{
    private readonly IWeeklyAssessmentRepository _weeklyAssessmentRepository =
        weeklyAssessmentRepository
        ?? throw new ArgumentNullException(nameof(weeklyAssessmentRepository));

    private readonly IStudyAreaWeekRepository _studyAreaWeekRepository =
        studyAreaWeekRepository
        ?? throw new ArgumentNullException(nameof(studyAreaWeekRepository));

    private readonly IStudyAreaWeekAssessmentRepository
        _studyAreaWeekAssessmentRepository =
            studyAreaWeekAssessmentRepository
            ?? throw new ArgumentNullException(
                nameof(studyAreaWeekAssessmentRepository));

    private readonly IStudyRecordRepository _studyRecordRepository =
        studyRecordRepository
        ?? throw new ArgumentNullException(nameof(studyRecordRepository));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar
        ?? throw new ArgumentNullException(nameof(isoWeekCalendar));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork
        ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task SynchronizeAsync(
        Guid weeklyAssessmentId,
        CancellationToken cancellationToken = default)
    {
        if (weeklyAssessmentId == Guid.Empty)
        {
            throw new ArgumentException("WeeklyAssessmentId is required.", nameof(weeklyAssessmentId));
        }

        var weeklyAssessment =
            await _weeklyAssessmentRepository.GetByIdAsync(weeklyAssessmentId, cancellationToken);

        if (weeklyAssessment is null)
        {
            throw new KeyNotFoundException($"WeeklyAssessment '{weeklyAssessmentId}' was not found.");
        }

        var weekStartDate =
            _isoWeekCalendar.GetMonday(
                weeklyAssessment.Year,
                weeklyAssessment.WeekNumber);

        var isoWeek = _isoWeekCalendar.GetIsoWeek(weekStartDate);

        if (isoWeek.Year != weeklyAssessment.Year || isoWeek.WeekNumber != weeklyAssessment.WeekNumber)
        {
            throw new DomainException("The WeeklyAssessment ISO week is inconsistent.");
        }

        var studyAreaWeeks =
            await _studyAreaWeekRepository.GetByWeekStartDateAsync
                (weekStartDate, cancellationToken);

        if (studyAreaWeeks.Count == 0)
        {
            throw new DomainException(
                $"No StudyAreaWeek configuration was found for " +
                $"ISO week {weeklyAssessment.Year}-W" +
                $"{weeklyAssessment.WeekNumber:D2}.");
        }

        var individualGoals = new List<decimal>(studyAreaWeeks.Count);

        var totalMinutesStudied = 0;

        foreach (var studyAreaWeek in studyAreaWeeks)
        {
            if (studyAreaWeek.WeeklyAssessmentId != weeklyAssessment.Id)
            {
                throw new DomainException("A StudyAreaWeek is associated with a WeeklyAssessment belonging to another week.");
            }

            var assessment =
                await _studyAreaWeekAssessmentRepository
                    .GetByStudyAreaWeekIdAsync(studyAreaWeek.Id, cancellationToken);

            if (assessment is null)
            {
                throw new DomainException(
                    $"StudyAreaWeekAssessment was not found for StudyAreaWeek '{studyAreaWeek.Id}'.");
            }

            if (assessment.StudyAreaWeekId != studyAreaWeek.Id)
            {
                throw new DomainException("The StudyAreaWeekAssessment does not belong to its StudyAreaWeek.");
            }

            var records =
                await _studyRecordRepository.GetByStudyAreaWeekIdAsync
                    (studyAreaWeek.Id, cancellationToken);

            var minutesStudied = 0;

            foreach (var record in records)
            {
                if (record.StudyAreaWeekId != studyAreaWeek.Id)
                {
                    throw new DomainException("A StudyRecord is associated with an unexpected StudyAreaWeek.");
                }

                minutesStudied = checked(minutesStudied + record.Minutes.Value);
            }

            assessment.UpdateMinutesStudied(minutesStudied);
            _studyAreaWeekAssessmentRepository.Update(assessment);
            individualGoals.Add(assessment.WeekIndividualGoal);
            totalMinutesStudied = checked(totalMinutesStudied + minutesStudied);
        }

        var globalGoal = GoalCalculator.CalculateGlobalGoal(individualGoals);

        WeeklyGoalValidator.Validate(globalGoal);
        weeklyAssessment.UpdateGlobalGoal(globalGoal);
        weeklyAssessment.UpdateMinutesStudied(totalMinutesStudied);
        _weeklyAssessmentRepository.Update(weeklyAssessment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}