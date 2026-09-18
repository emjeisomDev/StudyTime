using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Services;

public static class WeekDateValidator
{
    public static bool ValidateRecordDate(DateOnly recordDate, WeekRange weekRange)
        => weekRange.Contains(recordDate);
    
}