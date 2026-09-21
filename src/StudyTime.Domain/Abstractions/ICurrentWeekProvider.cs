using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Abstractions;

public interface ICurrentWeekProvider
{
    public WeekRange GetCurrentWeek();
    public DateOnly GetTodayInSaoPaulo();
}