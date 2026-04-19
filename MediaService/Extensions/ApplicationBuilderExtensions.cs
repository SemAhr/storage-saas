using MediaService.Api.Middleware;
using MediaService.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace MediaService.Extensions;

public static class ApplicationBuilderExtensions
{
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
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                if (exception is AppException appException)
                {
                    context.Response.StatusCode = appException.StatusCode;

                    await context.Response.WriteAsJsonAsync(new
                    {
                        title = appException.Title,
                        error = appException.Message
                    });

                    return;
                }

                if (exception is BadHttpRequestException badRequestException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;

                    await context.Response.WriteAsJsonAsync(new
                    {
                        title = "Bad Request",
                        error = badRequestException.Message.Contains("Failed to read parameter") ? "Invalid request body" : badRequestException.Message
                    });

                    return;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Internal Server Error",
                    error = "Unexpected error"
                });
            });
        });

        return app;
    }
}
