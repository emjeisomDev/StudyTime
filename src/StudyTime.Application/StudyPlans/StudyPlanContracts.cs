using StudyTime.Domain.Enums;

namespace StudyTime.Application.StudyPlans;

public sealed record CreateStudyPlanRequest(string? Name, decimal Coefficient);

public sealed record UpdateStudyPlanRequest(string? Name, decimal Coefficient);

public sealed record ChangeStudyPlanStatusRequest(StudyPlanStatus Status);

public sealed record StudyPlanResponse(Guid Id, string Name, decimal Coefficient, StudyPlanStatus Status);