namespace MediaService.Presentation.Middleware;

public sealed class TraceContextMiddleware(
    RequestDelegate next,
    ILogger<TraceContextMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<TraceContextMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = context.TraceIdentifier
        });

        await _next(context);
    }
}
