using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions;

public interface IStudyRecordLifoSelector
{
    public Task<StudyRecord?> SelectLastAsync(Guid studyAreaWeekId, CancellationToken token);
}
