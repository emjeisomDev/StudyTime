using Xunit;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.Services;

public sealed class GoalCalculatorTests
{
    private readonly GoalCalculator _sut = new();

    [Theory]
    [InlineData(100, 0.33, 33.00)]
    [InlineData(100, 0.335, 33.50)]
    [Trait("Rule", "R20")]
    public void CalculateIndividualGoal_ValidValues_ReturnsRoundedGoal(
        int standardMinutes,
        double coefficient,
        double expected)
    {
        var result = _sut.CalculateIndividualGoal(
            StdWeekMinutes.From(standardMinutes),
            PlanCoefficient.From((decimal)coefficient));

        Assert.Equal((decimal)expected, result);
    }

    [Fact]
    [Trait("Rule", "R20")]
    public void CalculateIndividualGoal_HalfCentAwayFromZero_RoundsUp()
    {
        var result = _sut.CalculateIndividualGoal(
            StdWeekMinutes.From(100),
            PlanCoefficient.From(0.335m));

        Assert.Equal(33.50m, result);
    }

    [Fact]
    [Trait("Rule", "R03")]
    public void CalculateIndividualGoal_InvalidStandardMinutes_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() =>
            _sut.CalculateIndividualGoal(
                default,
                PlanCoefficient.From(0.33m)));
    }

    [Fact]
    [Trait("Rule", "R13")]
    public void CalculateIndividualGoal_InvalidCoefficient_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() =>
            _sut.CalculateIndividualGoal(
                StdWeekMinutes.From(100),
                default));
    }

    [Fact]
    [Trait("Rule", "R12")]
    public void SumGlobalGoal_TwoItems_ReturnsSum()
    {
        var weeks = CreateWeeks(2);

        var result = _sut.SumGlobalGoal(
            weeks,
            _ => StdWeekMinutes.From(100),
            _ => PlanCoefficient.From(0.33m));

        Assert.Equal(66.00m, result);
    }

    [Fact]
    [Trait("Rule", "R12")]
    public void SumGlobalGoal_ThreeItems_ReturnsSum()
    {
        var weeks = CreateWeeks(3);

        var result = _sut.SumGlobalGoal(
            weeks,
            _ => StdWeekMinutes.From(100),
            _ => PlanCoefficient.From(0.33m));

        Assert.Equal(99.00m, result);
    }

    [Fact]
    [Trait("Rule", "R12")]
    public void SumGlobalGoal_EmptyCollection_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _sut.SumGlobalGoal(
                [],
                _ => StdWeekMinutes.From(100),
                _ => PlanCoefficient.From(0.33m)));
    }

    [Fact]
    [Trait("Rule", "R12")]
    public void SumGlobalGoal_NullCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _sut.SumGlobalGoal(
                null!,
                _ => StdWeekMinutes.From(100),
                _ => PlanCoefficient.From(0.33m)));
    }

    private static List<StudyAreaWeek> CreateWeeks(int count)
    {
        var weeks = new List<StudyAreaWeek>();

        for (var index = 0; index < count; index++)
        {
            weeks.Add(
                StudyAreaWeek.Create(
                    new DateOnly(2026, 9, 7),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid()));
        }

        return weeks;
    }
}