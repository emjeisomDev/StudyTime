using MediatR;
using Microsoft.Extensions.Logging;
using StudyTime.Domain.Abstractions;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger) :
    IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork
        ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommand)
        {
            return await next(cancellationToken);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch (Exception originalException)
        {
            try
            {
                // A rollback must not replace the exception that caused the transaction to fail.
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                _logger.LogError(
                    rollbackException,
                    "Rollback failed after command {CommandType} failed. The original exception will be rethrown. OriginalExceptionType: {OriginalExceptionType}",
                    typeof(TRequest).Name,
                    originalException.GetType().FullName);
            }

            throw;
        }
    }
}