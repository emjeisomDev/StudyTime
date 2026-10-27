using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StudyTime.Api.Filters;

/// <summary>Uses the RFC 9457 media type for MVC ProblemDetails results.</summary>
public sealed class ProblemDetailsContentTypeFilter : IAsyncResultFilter, IOrderedFilter
{
    // Run after action-level [Produces("application/json")] filters so ProblemDetails keeps RFC 9457 content type.
    public int Order => int.MaxValue;

    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Result is ObjectResult objectResult &&
            objectResult.Value is ProblemDetails)
        {
            objectResult.ContentTypes.Clear();
            objectResult.ContentTypes.Add("application/problem+json");
        }

        await next();
    }
}