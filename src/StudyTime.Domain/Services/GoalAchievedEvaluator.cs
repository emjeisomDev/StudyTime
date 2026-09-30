using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Services;

public static class GoalAchievedEvaluator
{
    public static bool EvaluateIndividual(
        StudyAreaWeekAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        return assessment.MinutesStudied >= assessment.WeekIndividualGoal;
    }

    public static bool EvaluateGlobal(
        IEnumerable<StudyAreaWeekAssessment> assessments)
    {
        ArgumentNullException.ThrowIfNull(assessments);

        bool hasAssessments = false;

        foreach (StudyAreaWeekAssessment assessment in assessments)
        {
            ArgumentNullException.ThrowIfNull(assessment);

            hasAssessments = true;

            if (!EvaluateIndividual(assessment))
            {
                return false;
            }
        }

        return hasAssessments;
    }
}