using MediatR;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyRecords.Queries;

public sealed class GetStudyRecordsByWeekHandler(
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyRecordRepository studyRecordRepository)
    : IRequestHandler<GetStudyRecordsByWeekQuery, IReadOnlyList<StudyRecordDto>>
{
    private readonly IStudyAreaWeekRepository _studyAreaWeekRepository =
        studyAreaWeekRepository
        ?? throw new ArgumentNullException(nameof(studyAreaWeekRepository));

    private readonly IStudyRecordRepository _studyRecordRepository =
        studyRecordRepository
        ?? throw new ArgumentNullException(nameof(studyRecordRepository));

    public async Task<IReadOnlyList<StudyRecordDto>> Handle(
        GetStudyRecordsByWeekQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.StudyAreaWeekId == Guid.Empty)
        {
            throw new ArgumentException(
                "StudyAreaWeekId is required.",
                nameof(request.StudyAreaWeekId));
        }

        var studyAreaWeek =
            await _studyAreaWeekRepository.GetByIdAsync(
                request.StudyAreaWeekId,
                cancellationToken);

        if (studyAreaWeek is null)
        {
            throw new KeyNotFoundException(
                $"StudyAreaWeek '{request.StudyAreaWeekId}' was not found.");
        }

        var records =
            await _studyRecordRepository.GetByStudyAreaWeekIdAsync(
                studyAreaWeek.Id,
                cancellationToken);

        return records
            .OrderBy(record => record.CreatedAt)
            .ThenBy(record => record.Id)
            .Select(
                record => new StudyRecordDto(
                    record.Id,
                    record.Date,
                    record.CreatedAt,
                    record.Minutes.Value,
                    record.StudyAreaWeekId))
            .ToArray();
    }
}