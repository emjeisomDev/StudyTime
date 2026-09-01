namespace StudyTime.Application.StudyAreas;

public sealed record CreateStudyAreaRequest(string? Name, int StdWeekStudyTime);

public sealed record UpdateStudyAreaRequest(string? Name, int StdWeekStudyTime);

public sealed record StudyAreaResponse(Guid Id, string Name, int StdWeekStudyTime);