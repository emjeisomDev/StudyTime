using StudyTime.Domain.Entities;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Application.Tests.Fixtures;

public sealed class StudyTimeFixture
{
    public const string DefaultStudyAreaName = "Matemática";
    public const int DefaultWeeklyStudyTime = 300;

    public FakeClock Clock { get; } = new(
        new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

    public Guid StudyAreaId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public StudyArea CreateStudyArea(
        string name = DefaultStudyAreaName,
        int weeklyStudyTime = DefaultWeeklyStudyTime)
    {
        return new StudyArea(
            StudyAreaId,
            name,
            new Minutes(weeklyStudyTime));
    }
}