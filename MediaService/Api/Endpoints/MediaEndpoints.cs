using MediaService.Application.Media;
using MediaService.Contracts.Media;

namespace MediaService.Api.Endpoints;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/file").WithTags("Media");

        group.MapGet("/", async (IMediaService service, CancellationToken cancellationToken) =>
        {
            var file = await service.GetAllAsync(cancellationToken);
            return Results.Ok(file);
        });

        return app;
    }
}
