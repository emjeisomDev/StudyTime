using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyRecords.Queries;

public sealed record GetStudyRecordsByWeekQuery(Guid StudyAreaWeekId)
    : IRequest<IReadOnlyList<StudyRecordDto>>;