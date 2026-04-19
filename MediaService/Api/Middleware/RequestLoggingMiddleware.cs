using System.Diagnostics;

namespace MediaService.Api.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Incoming request: {Method} {Path}{QueryString}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString
        );

        await _next(context);

        stopwatch.Stop();

        _logger.LogInformation(
            "Completed request: {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds
        );
    }
}
