using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;

namespace StudyTime.Domain.Tests.Services;

public sealed class GoalAchievedEvaluatorTests
{
    [Fact]
    public void EvaluateIndividual_WhenMinutesEqualGoal_ReturnsTrue()
    {
        StudyAreaWeekAssessment assessment = CreateAssessment(
            goal: 100m,
            minutesStudied: 100);

        bool result = GoalAchievedEvaluator.EvaluateIndividual(assessment);

        Assert.True(result);
    }

    [Fact]
    public void EvaluateIndividual_WhenMinutesExceedGoal_ReturnsTrue()
    {
        StudyAreaWeekAssessment assessment = CreateAssessment(
            goal: 100m,
            minutesStudied: 120);

        bool result = GoalAchievedEvaluator.EvaluateIndividual(assessment);

        Assert.True(result);
    }

    [Fact]
    public void EvaluateIndividual_WhenMinutesAreBelowGoal_ReturnsFalse()
    {
        StudyAreaWeekAssessment assessment = CreateAssessment(
            goal: 100m,
            minutesStudied: 99);

        bool result = GoalAchievedEvaluator.EvaluateIndividual(assessment);

        Assert.False(result);
    }

    [Fact]
    public void EvaluateIndividual_WhenAssessmentIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => GoalAchievedEvaluator.EvaluateIndividual(null!));
    }

    [Fact]
    public void EvaluateGlobal_WhenAllAssessmentsMeetGoals_ReturnsTrue()
    {
        StudyAreaWeekAssessment[] assessments =
        [
            CreateAssessment(100m, 100),
            CreateAssessment(200m, 250),
            CreateAssessment(300m, 300)
        ];

        bool result = GoalAchievedEvaluator.EvaluateGlobal(assessments);

        Assert.True(result);
    }

    [Fact]
    public void EvaluateGlobal_WhenAnyAssessmentDoesNotMeetGoal_ReturnsFalse()
    {
        StudyAreaWeekAssessment[] assessments =
        [
            CreateAssessment(100m, 100),
            CreateAssessment(200m, 199),
            CreateAssessment(300m, 350)
        ];

        bool result = GoalAchievedEvaluator.EvaluateGlobal(assessments);

        Assert.False(result);
    }

    [Fact]
    public void EvaluateGlobal_WhenAssessmentsAreEmpty_ReturnsFalse()
    {
        StudyAreaWeekAssessment[] assessments = [];

        bool result = GoalAchievedEvaluator.EvaluateGlobal(assessments);

        Assert.False(result);
    }

    [Fact]
    public void EvaluateGlobal_WhenAssessmentsAreNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => GoalAchievedEvaluator.EvaluateGlobal(null!));
    }

    [Fact]
    public void EvaluateGlobal_WhenAnAssessmentIsNull_ThrowsArgumentNullException()
    {
        StudyAreaWeekAssessment[] assessments =
        [
            CreateAssessment(100m, 100),
            null!
        ];

        Assert.Throws<ArgumentNullException>(
            () => GoalAchievedEvaluator.EvaluateGlobal(assessments));
    }

    private static StudyAreaWeekAssessment CreateAssessment(
        decimal goal,
        int minutesStudied)
    {
        return new StudyAreaWeekAssessment(
            weekIndividualGoal: goal,
            minutesStudied: minutesStudied,
            studyAreaWeekId: Guid.NewGuid());
    }
}