using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions;

/// <summary>
/// Selects the most recently created eligible study record.
/// </summary>
public interface IStudyRecordLifoSelector
{
    public Task<StudyRecord?> SelectLastEligibleAsync
        (Guid studyAreaWeekId, CancellationToken cancellationToken = default);
}