using FluentValidation;
using Myrati.Application.Common.Exceptions;

namespace Myrati.API.Middleware;

public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request cancelled by client");
        }
        catch (IOException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request stream closed by client");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception");

            if (context.Response.HasStarted)
            {
                logger.LogWarning("Response already started. Skipping error payload.");
                return;
            }

            await WriteResponseAsync(context, exception);
        }
    }

    private static Task WriteResponseAsync(HttpContext context, Exception exception)
    {
        int status;
        string title;
        string detail;
        object? errors;

        switch (exception)
        {
            case ValidationException validationException:
                status = StatusCodes.Status400BadRequest;
                title = "Validation error";
                detail = "Um ou mais campos são inválidos.";
                errors = validationException.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(x => x.ErrorMessage).ToArray());
                break;
            case EntityNotFoundException:
                status = StatusCodes.Status404NotFound;
                title = "Not found";
                detail = exception.Message;
                errors = null;
                break;
            case ConflictException:
                status = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = exception.Message;
                errors = null;
                break;
            case UnauthorizedAccessException:
                status = StatusCodes.Status401Unauthorized;
                title = "Unauthorized";
                detail = exception.Message;
                errors = null;
                break;
            default:
                status = StatusCodes.Status500InternalServerError;
                title = "Internal server error";
                detail = "Ocorreu um erro inesperado ao processar a requisição.";
                errors = null;
                break;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new
        {
            status,
            title,
            detail,
            errors
        });
    }
}
