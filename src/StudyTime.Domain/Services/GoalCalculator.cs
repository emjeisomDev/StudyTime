using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Services;

public class GoalCalculator : IGoalCalculator
{
    public decimal CalculateIndividualGoal(StdWeekMinutes std, PlanCoefficient coeff)
    {
        if (std.Value <= 0)
        {
            throw DomainRuleViolationException.R03_StdWeekStudyTimeMustBePositive();
        }

        if (coeff.Value <= 0)
        {
            throw DomainRuleViolationException.R13_CoefficientMustBePositive();
        }

        return decimal.Round(std.Value * coeff.Value, 2, MidpointRounding.AwayFromZero);
    }

    public decimal SumGlobalGoal(
        IEnumerable<StudyAreaWeek> weeks,
        Func<StudyAreaWeek, StdWeekMinutes> resolveStd,
        Func<StudyAreaWeek, PlanCoefficient> resolveCoeff)
    {
        ArgumentNullException.ThrowIfNull(weeks);
        ArgumentNullException.ThrowIfNull(resolveStd);
        ArgumentNullException.ThrowIfNull(resolveCoeff);

        var weekList = weeks.ToList();

        if (weekList.Count == 0)
        {
            throw new ArgumentException("The weekly configuration collection cannot be empty.", nameof(weeks));
        }

        decimal total = 0m;

        foreach (var week in weekList)
        {
            ArgumentNullException.ThrowIfNull(week);

            var std = resolveStd(week);
            var coeff = resolveCoeff(week);

            total += CalculateIndividualGoal(std, coeff);
        }

        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }
}