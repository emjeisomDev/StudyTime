using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Abstractions;

public interface IIsoWeekCalendar
{
    public IsoWeek FromDate(DateOnly weekStartDate);
}
