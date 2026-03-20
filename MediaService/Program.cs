using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MediaService.Api.Endpoints;
using MediaService.Data;
using MediaService.Domain.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
             ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres");

    opt.UseNpgsql(connectionString);
});

builder.Services.AddScoped<MediaService.Application.File.IFileService, MediaService.Application.File.FileService>();
builder.Services.AddScoped<MediaService.Application.Node.NodeService, MediaService.Application.Node.NodeService>();

var app = builder.Build();

app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (ex is AppException appEx)
        {
            context.Response.StatusCode = appEx.StatusCode;

            await context.Response.WriteAsJsonAsync(new
            {
                title = appEx.Title,
                status = appEx.StatusCode,
                error = appEx.Message
            });

            return;
        }

        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Internal Server Error",
            status = 500,
            error = "Unexpected error"
        });
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFileEndpoints();

app.Urls.Add("http://localhost:3000");
app.Run();
