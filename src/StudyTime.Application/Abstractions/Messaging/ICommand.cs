using MediatR;

namespace StudyTime.Application.Abstractions.Messaging;

public interface ICommand : IUseCase { }
public interface ICommand<out TResponse> : ICommand, IRequest<TResponse> { }