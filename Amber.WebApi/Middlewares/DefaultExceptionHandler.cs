using Amber.Application.Sync.Commands;
using Amber.WebApi.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Amber.WebApi.Middlewares;

internal sealed class DefaultExceptionHandler<T>(ILogger<T> logger) : IExceptionHandler
    where T : Exception
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not T)
        {
            return false;
        }
        logger.LogInformation(exception, "Exception occured: {Message}.", exception.Message);

        var statusCode = exception switch
        {
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            InternalErrorException => StatusCodes.Status500InternalServerError,
            InsufficientStorageException => StatusCodes.Status507InsufficientStorage,
            _ => StatusCodes.Status400BadRequest,
        };
        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Detail = exception.Message, Status = statusCode },
            cancellationToken
        );

        return true;
    }
}
