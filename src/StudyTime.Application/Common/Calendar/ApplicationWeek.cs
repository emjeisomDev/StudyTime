namespace StudyTime.Application.Common.Calendar;

public readonly record struct ApplicationWeek
{
    public DateOnly WeekStartDate { get; }
    public DateOnly WeekEndDate { get; }
    public int IsoYear { get; }
    public int IsoWeek { get; }
    
    public ApplicationWeek(DateOnly weekStartDate)
    {
        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("The week start date must be a Monday.", nameof(weekStartDate));

        WeekStartDate = weekStartDate;
        WeekEndDate = weekStartDate.AddDays(6);
        IsoYear = System.Globalization.ISOWeek.GetYear(weekStartDate);
        IsoWeek = System.Globalization.ISOWeek.GetWeekOfYear(weekStartDate);
    }

    public ApplicationWeek AddWeeks(int weeks)
        => new(WeekStartDate.AddDays(weeks * 7));
}