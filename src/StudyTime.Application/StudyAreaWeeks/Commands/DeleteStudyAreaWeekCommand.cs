using MediatR;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed record DeleteStudyAreaWeekCommand(Guid Id)
    : ICommand, IRequest<Unit>;