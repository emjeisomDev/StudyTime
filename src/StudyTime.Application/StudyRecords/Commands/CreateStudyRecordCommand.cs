using MediatR;
using StudyTime.Application.Dtos;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyRecords.Commands;

public sealed record CreateStudyRecordCommand(
    Guid StudyAreaWeekId,
    int Minutes)
    : ICommand, IRequest<StudyRecordDto>;