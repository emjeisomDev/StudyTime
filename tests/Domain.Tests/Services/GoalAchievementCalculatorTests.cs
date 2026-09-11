using Xunit;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Tests.Services;

public sealed class GoalAchievementCalculatorTests
{
    private readonly GoalAchievementCalculator _sut = new();
    private static readonly Guid WeekId = Guid.NewGuid();

    [Fact]
    public void IsIndividualGoalAchieved_MinutesEqualGoal_ReturnsTrue()
    {
        var result = _sut.IsIndividualGoalAchieved(100, 100m);
        Assert.True(result);
    }

    [Fact]
    public void IsIndividualGoalAchieved_MinutesAboveGoal_ReturnsTrue()
    {
        var result = _sut.IsIndividualGoalAchieved(101, 100m);
        Assert.True(result);
    }

    [Fact]
    public void IsIndividualGoalAchieved_MinutesBelowGoal_ReturnsFalse()
    {
        var result = _sut.IsIndividualGoalAchieved(99, 100m);
        Assert.False(result);
    }

    [Fact]
    public void IsIndividualGoalAchieved_NegativeMinutes_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.IsIndividualGoalAchieved(-1, 100m));
    }

    [Fact]
    public void IsIndividualGoalAchieved_ZeroGoal_ThrowsArgumentOutOfRangeException()
    {
        //Assert.Throws<ArgumentOutOfRangeException>(() => _sut.IsIndividualGoalAchieved(100, 0m));
        // var result = _sut.IsIndividualGoalAchieved(100, 0m);
        // Assert.True(result);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _sut.IsIndividualGoalAchieved(100, 0m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsIndividualGoalAchieved_NonPositiveGoal_ThrowsArgumentOutOfRangeException(
        decimal weekIndividualGoal)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _sut.IsIndividualGoalAchieved(100, weekIndividualGoal));
    }

    [Fact]
    public void IsIndividualGoalAchieved_PositiveGoal_ReturnsExpectedResult()
    {
        var result = _sut.IsIndividualGoalAchieved(100, 100m);

        Assert.True(result);
    }

    [Fact]
    public void IsGlobalGoalAchieved_AllTrue_ReturnsTrue()
    {
        var result = _sut.IsGlobalGoalAchieved([true, true, true]);
        Assert.True(result);
    }

    [Fact]
    public void IsGlobalGoalAchieved_OneFalse_ReturnsFalse()
    {
        var result = _sut.IsGlobalGoalAchieved([true, false, true]);
        Assert.False(result);
    }

    [Fact]
    [Trait("Rule", "R24")]
    public void R24_IsGlobalGoalAchieved_ThrowsForEmptyCollection()
    {
        // var result = _sut.IsGlobalGoalAchieved([]);
        // Assert.False(result);
        Assert.Throws<DomainRuleViolationException>(
            () => _sut.IsGlobalGoalAchieved(Array.Empty<bool>()));
    }

    [Fact]
    [Trait("Rule", "R24")]
    public void IsGlobalGoalAchieved_NullCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.IsGlobalGoalAchieved(null!));
    }

    [Fact]
    [Trait("Rule", "R26")]
    public void IsGlobalGoalAchieved_IndividualAssessmentsAllAchieved_ReturnsTrue()
    {
        var individualAchievements = new[]
        {
            _sut.IsIndividualGoalAchieved(100, 100m),
            _sut.IsIndividualGoalAchieved(200, 100m)
        };

        var result = _sut.IsGlobalGoalAchieved(individualAchievements);

        Assert.True(result);
    }

    [Fact]
    [Trait("Rule", "R26")]
    public void IsGlobalGoalAchieved_IndividualAssessmentsOneNotAchieved_ReturnsFalse()
    {
        var individualAchievements = new[]
        {
            _sut.IsIndividualGoalAchieved(100, 100m),
            _sut.IsIndividualGoalAchieved(50, 100m)
        };

        var result = _sut.IsGlobalGoalAchieved(individualAchievements);

        Assert.False(result);
    }

    [Fact]
    [Trait("Rule", "R15")]
    public void IsLastRecord_GreaterCreatedAt_ReturnsTrueForNewestRecord()
    {
        var older = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            30,
            WeekId,
            new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero),
            Guid.Parse("00000000-0000-0000-0000-000000000001"));

        var newer = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            30,
            WeekId,
            new DateTimeOffset(2026, 9, 7, 11, 0, 0, TimeSpan.Zero),
            Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var result = _sut.IsLastRecord(newer, [older, newer]);

        Assert.True(result);
    }

    [Fact]
    [Trait("Rule", "R15")]
    public void IsLastRecord_OlderCreatedAt_ReturnsFalseForOlderRecord()
    {
        var older = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            30,
            WeekId,
            new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero),
            Guid.Parse("00000000-0000-0000-0000-000000000001"));

        var newer = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            30,
            WeekId,
            new DateTimeOffset(2026, 9, 7, 11, 0, 0, TimeSpan.Zero),
            Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var result = _sut.IsLastRecord(older, [older, newer]);
        Assert.False(result);
    }

    [Fact]
    [Trait("Rule", "R15")]
    public void IsLastRecord_SameCreatedAt_HigherIdWins()
    {
        var lowerId = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            30,
            WeekId,
            new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero),
            Guid.Parse("00000000-0000-0000-0000-000000000001"));

        var higherId = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            30,
            WeekId,
            new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero),
            Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var result = _sut.IsLastRecord(higherId, [lowerId, higherId]);
        Assert.True(result);
    }

    [Fact]
    [Trait("Rule", "R15")]
    public void IsLastRecord_SameCreatedAt_LowerIdLoses()
    {
        var lowerId = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            30,
            WeekId,
            new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero),
            Guid.Parse("00000000-0000-0000-0000-000000000001"));

        var higherId = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            30,
            WeekId,
            new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero),
            Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var result = _sut.IsLastRecord(lowerId, [lowerId, higherId]);
        Assert.False(result);
    }

    [Fact]
    [Trait("Rule", "R15")]
    public void IsLastRecord_EmptyCollection_ReturnsFalse()
    {
        var record = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            30,
            WeekId,
            new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero));

        var result = _sut.IsLastRecord(record, []);
        Assert.False(result);
    }
}