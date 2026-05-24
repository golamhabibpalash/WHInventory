using ASPNET.BackEnd.Common.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace ASPNET.BackEnd.Common.Handlers;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;
    private readonly IHostEnvironment _environment;

    public CustomExceptionHandler(IHostEnvironment environment)
    {
        _environment = environment;
        _exceptionHandlers = new()
            {
                { typeof(Exception), HandleException },
            };
    }
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (_exceptionHandlers.ContainsKey(exceptionType))
        {
            await _exceptionHandlers[exceptionType].Invoke(httpContext, exception);
            return true;
        }
        else
        {
            await HandleException(httpContext, exception);
            return true;
        }

    }

    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        var statusCode = httpContext.Response.StatusCode != 200
            ? httpContext.Response.StatusCode
            : StatusCodes.Status500InternalServerError;

        // Only expose internal exception details (stack trace, source, inner message)
        // in development. In production these leak implementation details to clients.
        var includeDetails = _environment.IsDevelopment();

        var errorMessage = includeDetails
            ? ex.Message
            : "An unexpected error occurred while processing your request.";

        var error = includeDetails
            ? new Error(ex.InnerException?.Message, ex.Source, ex.StackTrace, ex.GetType().Name)
            : new Error(null, null, null, null);

        var result = new ApiErrorResult
        {
            Code = statusCode,
            Message = $"Exception: {errorMessage}",
            Error = error
        };

        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(result);
    }

}

