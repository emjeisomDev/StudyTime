using MediatR;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyRecords.Commands;

public sealed record DeleteLastStudyRecordCommand(Guid StudyAreaWeekId)
    : ICommand, IRequest<Unit>;