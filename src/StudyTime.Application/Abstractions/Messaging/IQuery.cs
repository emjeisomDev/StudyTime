using MediatR;

namespace StudyTime.Application.Abstractions.Messaging;

public interface IQuery<out TResponse>
    : IUseCase, IRequest<TResponse>
{
}