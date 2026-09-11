using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Services;

public sealed class GoalAchievementCalculator : IGoalAchievementCalculator
{
    public bool IsIndividualGoalAchieved(int minutesStudied, decimal weekIndividualGoal)
    {
        if (minutesStudied < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutesStudied), "Minutes studied cannot be negative.");
        }

        if (weekIndividualGoal <= 0)
        {
            throw new ArgumentOutOfRangeException
                (nameof(weekIndividualGoal),
                "The weekly individual goal must be greater than zero.");
        }

        return minutesStudied >= weekIndividualGoal;
    }

    public bool IsGlobalGoalAchieved(IEnumerable<bool> individualAchievements)
    {
        ArgumentNullException.ThrowIfNull(individualAchievements);

        var achievements = individualAchievements.ToList();
        if (achievements.Count == 0)
        {
            throw DomainRuleViolationException.R24_GlobalGoalRequiresAtLeastOneStudyAreaWeek();
        }

        return achievements.All(x => x);
    }

    public bool IsLastRecord(StudyRecord candidate, IEnumerable<StudyRecord> all)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(all);

        var records = all.ToList();

        if (records.Count == 0)
        {
            return false;
        }

        if (records.Any(record => record is null))
        {
            throw new ArgumentException("The record collection cannot contain null items.", nameof(all));
        }

        var lastRecord = records
            .OrderByDescending(record => record.CreatedAt)
            .ThenByDescending(record => record.Id)
            .First();

        return candidate.Id == lastRecord.Id;
    }
}