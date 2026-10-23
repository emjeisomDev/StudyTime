using MediatR;

namespace StudyTime.Application.Abstractions.Messaging;

public interface ICommand : IUseCase, IRequest
{
}

public interface ICommand<out TResponse> : ICommand, IRequest<TResponse>
{
}