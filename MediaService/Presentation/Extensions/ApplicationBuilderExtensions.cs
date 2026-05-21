using MediaService.Domain.Exceptions;
using MediaService.Presentation.Middleware;
using Microsoft.AspNetCore.Diagnostics;

namespace MediaService.Presentation.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTraceContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TraceContextMiddleware>();
    }

    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }

    public static IApplicationBuilder UseAppExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(handler =>
        {
            handler.Run(async context =>
            {
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalExceptionHandler");

                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                context.Response.ContentType = "application/json";

                if (exception is AppException appException)
                {
                    var statusCode = MapStatusCode(appException.ErrorType);
                    var title = MapTitle(appException.ErrorType);

                    context.Response.StatusCode = statusCode;

                    if (statusCode >= StatusCodes.Status500InternalServerError)
                    {
                        logger.LogError(
                            exception,
                            "Application exception handled. ErrorType: {ErrorType}, TraceId: {TraceId}",
                            appException.ErrorType,
                            context.TraceIdentifier);
                    }
                    else
                    {
                        logger.LogWarning(
                            exception,
                            "Application exception handled. ErrorType: {ErrorType}, TraceId: {TraceId}",
                            appException.ErrorType,
                            context.TraceIdentifier);
                    }

                    await context.Response.WriteAsJsonAsync(new
                    {
                        title,
                        error = statusCode >= StatusCodes.Status500InternalServerError
                            ? "Unexpected error"
                            : appException.Message,
                        traceId = context.TraceIdentifier
                    });

                    return;
                }

                if (exception is BadHttpRequestException badRequestException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;

                    logger.LogWarning(
                        badRequestException,
                        "Bad HTTP request handled. TraceId: {TraceId}",
                        context.TraceIdentifier);

                    await context.Response.WriteAsJsonAsync(new
                    {
                        title = "Bad Request",
                        error = IsInvalidRequestBodyError(badRequestException)
                            ? "Invalid request body"
                            : badRequestException.Message,
                        traceId = context.TraceIdentifier
                    });

                    return;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                logger.LogError(
                    exception,
                    "Unhandled exception. TraceId: {TraceId}",
                    context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Internal Server Error",
                    error = "Unexpected error",
                    traceId = context.TraceIdentifier
                });
            });
        });

        return app;
    }

    private static int MapStatusCode(AppErrorType errorType)
    {
        return errorType switch
        {
            AppErrorType.Validation => StatusCodes.Status400BadRequest,
            AppErrorType.NotFound => StatusCodes.Status404NotFound,
            AppErrorType.Conflict => StatusCodes.Status409Conflict,
            AppErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            AppErrorType.Forbidden => StatusCodes.Status403Forbidden,
            AppErrorType.ExternalDependency => StatusCodes.Status503ServiceUnavailable,
            AppErrorType.Infrastructure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string MapTitle(AppErrorType errorType)
    {
        return errorType switch
        {
            AppErrorType.Validation => "Validation Error",
            AppErrorType.NotFound => "Not Found",
            AppErrorType.Conflict => "Conflict",
            AppErrorType.Unauthorized => "Unauthorized",
            AppErrorType.Forbidden => "Forbidden",
            AppErrorType.ExternalDependency => "Service Unavailable",
            AppErrorType.Infrastructure => "Internal Server Error",
            _ => "Internal Server Error"
        };
    }

    private static bool IsInvalidRequestBodyError(BadHttpRequestException exception)
    {
        return exception.Message.Contains(
            "Failed to read parameter",
            StringComparison.OrdinalIgnoreCase);
    }
}
