using System.Net;
using System.Text.Json;

namespace MinimalApi.Middleware;

/// <summary>
/// Middleware to handle exceptions and return appropriate HTTP status codes
/// </summary>
public partial class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogUnauthorizedAccess(logger, ex, ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            LogInvalidOperation(logger, ex, ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unauthorized access attempt: {Message}")]
    private static partial void LogUnauthorizedAccess(ILogger logger, Exception exception, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid operation: {Message}")]
    private static partial void LogInvalidOperation(ILogger logger, Exception exception, string message);

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            statusCode = (int)statusCode,
            message = exception.Message
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
