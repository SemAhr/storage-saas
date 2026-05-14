using MediaService.Application.Extensions;
using MediaService.Presentation.Endpoints.Files;
using MediaService.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

app.UseRequestLogging();
app.UseAppExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFileEndpoints();

app.Urls.Add("http://localhost:3000");
app.Run();
