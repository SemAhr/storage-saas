using System.Text.Json;
using MediaService.Api.Endpoints;
using MediaService.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddAppServices(builder.Configuration)
    // json policy to camelCase for consistency with JavaScript clients
    .ConfigureHttpJsonOptions(options => { options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; });

var app = builder.Build();

app.UseRequestLogging();
app.UseAppExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFileEndpoints();

app.Urls.Add("http://localhost:3000");
app.Run();
