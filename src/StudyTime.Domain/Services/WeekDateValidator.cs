using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Services;

public static class WeekDateValidator
{
    public static bool IsDateWithinWeek(DateOnly date, DateOnly weekStartDate)
    {
        WeekRange weekRange = new(weekStartDate);
        return date >= DateOnly.FromDateTime(weekRange.Start) && date <= DateOnly.FromDateTime(weekRange.End);
    }

    public static void ValidateDate(DateOnly date, DateOnly weekStartDate)
    {
        if (!IsDateWithinWeek(date, weekStartDate))
        {
            throw new ArgumentOutOfRangeException(
                nameof(date),
                date,
                "The record date must be within the configured week.");
        }
    }

    public static bool IsValidConfiguration(decimal globalGoal)
    {
        return globalGoal >= 1500m;
    }

    public static void ValidateConfiguration(decimal globalGoal)
    {
        if (!IsValidConfiguration(globalGoal))
        {
            throw new ArgumentException(
                "The weekly configuration must have a global goal of at least 1500 minutes.",
                nameof(globalGoal));
        }
    }

    public static void ValidateRecord(DateOnly date, DateOnly weekStartDate, decimal globalGoal)
    {
        ValidateDate(date, weekStartDate);
        ValidateConfiguration(globalGoal);
    }
}