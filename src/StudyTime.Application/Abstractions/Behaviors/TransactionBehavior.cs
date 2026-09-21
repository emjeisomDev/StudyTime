using MediatR;

using StudyTime.Domain.Abstractions;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.Abstractions.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
        => _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken token)
    {
        if (request is not ICommand)
        {
            return await next();
        }

        try
        {
            TResponse response = await next();
            await _unitOfWork.CommitAsync(token);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(token);
            throw;
        }
    }
}