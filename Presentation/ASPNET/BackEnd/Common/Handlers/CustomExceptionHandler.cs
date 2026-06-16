using ASPNET.BackEnd.Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

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
                { typeof(ValidationException), HandleValidationException },
                { typeof(DbUpdateException), HandleDbUpdateException },
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

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        var validationException = (ValidationException)ex;

        var errorMessage = validationException.Errors.Any()
            ? string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage))
            : validationException.Message;

        var result = new ApiErrorResult
        {
            Code = StatusCodes.Status400BadRequest,
            Message = errorMessage,
            Error = null
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(result);
    }

    private async Task HandleDbUpdateException(HttpContext httpContext, Exception ex)
    {
        var statusCode = StatusCodes.Status409Conflict;
        var errorMessage = "A database conflict occurred. The record may have been modified or already exists.";

        if (_environment.IsDevelopment())
        {
            errorMessage = ex.InnerException?.Message ?? ex.Message;
        }

        var result = new ApiErrorResult
        {
            Code = statusCode,
            Message = errorMessage,
            Error = null
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(result);
    }

    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        var statusCode = httpContext.Response.StatusCode != 200
            ? httpContext.Response.StatusCode
            : StatusCodes.Status500InternalServerError;

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

