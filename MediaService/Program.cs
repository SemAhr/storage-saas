using MediaService.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

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
                statusCode = appException.StatusCode,
                error = appException.Title,
                message = appException.Message,
            });
            return;
        }

        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new
        {
            statusCode = 500,
            error = "Internal Server Error",
            message = "An unexpected error occurred.",
        });
    });
});

// app.MapGet("/", () => "Hello World!");

app.Run();
