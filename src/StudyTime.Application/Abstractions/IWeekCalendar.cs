namespace StudyTime.Application.Abstractions;

public interface IWeekCalendar
{
    public DateOnly GetCurrentWeekStart();
    public DateOnly GetPreviousWeekStart();
    public DateOnly GetNextWeekStart();
    public (int Year, int WeekNumber) GetIsoYearWeek(DateOnly date);
    public bool IsMonday(DateOnly date);
    public DateOnly GetSundayOf(DateOnly weekStart);
}
