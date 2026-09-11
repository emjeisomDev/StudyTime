using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Services;

public interface IGoalAchievementCalculator
{
    public bool IsIndividualGoalAchieved(int minutesStudied, decimal weekIndividualGoal);
    public bool IsGlobalGoalAchieved(IEnumerable<bool> individualAchievements);
    public bool IsLastRecord(StudyRecord candidate, IEnumerable<StudyRecord> all);
}