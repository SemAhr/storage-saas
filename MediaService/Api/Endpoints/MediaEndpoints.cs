using MediaService.Application.Media;
using MediaService.Contracts.Media;

namespace MediaService.Api.Endpoints;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/media").WithTags("Media");

        group.MapGet("/", async (IMediaService service, CancellationToken cancellationToken) =>
        {
            var media = await service.GetAllAsync(cancellationToken);
            return Results.Ok(media);
        });

        return app;
    }
}
