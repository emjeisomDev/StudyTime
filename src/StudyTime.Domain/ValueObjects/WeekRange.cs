using StudyTime.Domain.Constants;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.ValueObjects;

public sealed record WeekRange
{
    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    private WeekRange(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public static WeekRange FromStartDate(DateOnly startDate)
    {
        if (startDate.DayOfWeek != DayOfWeek.Monday)
        {
            throw new DomainValidationException("The week start date must be a Monday.");
        }

        TimeZoneInfo timeZone = GetSaoPauloTimeZone();

        DateTime startLocal = startDate.ToDateTime(TimeOnly.MinValue);
        DateTime endLocal = startDate
            .AddDays(6)
            .ToDateTime(new TimeOnly(23, 59, 59));

        DateTimeOffset start = new(
            TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified),
                timeZone));

        DateTimeOffset end = new(
            TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified),
                timeZone));

        return new WeekRange(start, end);
    }

    public bool Contains(DateOnly date)
    {
        DateOnly startDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(Start, GetSaoPauloTimeZone()).DateTime);

        DateOnly endDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(End, GetSaoPauloTimeZone()).DateTime);

        return date >= startDate && date <= endDate;
    }

    private static TimeZoneInfo GetSaoPauloTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZones.SaoPaulo);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }
}