using MediaService.Application.Files;
using MediaService.Presentation.Contracts.Files;
using Microsoft.AspNetCore.Mvc;

namespace MediaService.Presentation.Endpoints.Files;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/files").WithTags("Files");

        group.MapPost("/uploads",
            async (
                [FromBody] UploadDto presignedRequestDto,
                IFileService fileService,
                CancellationToken cancellationToken
            ) =>
                {
                    var result = await fileService.UploadAsync(presignedRequestDto, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("PresignedUpload").WithDescription("Get a presigned URL for uploading a file.");

        group.MapPost("/uploads/{sessionId:guid}/confirm",
            async (
                [FromRoute] Guid sessionId,
                IFileService fileService,
                CancellationToken cancellationToken
            ) =>
                {
                    var result = await fileService.ConfirmUploadAsync(sessionId, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("ConfirmUpload").WithDescription("Confirm that a file has been uploaded successfully.");

        // --- Multipart Uploads
        group.MapGet("/uploads/{sessionId:guid}/parts",
            async (
                [FromRoute] Guid sessionId,
                [FromQuery(Name = "from")] int from,
                [FromQuery(Name = "to")] int to,
                IFileService fileService,
                CancellationToken cancellationToken
            ) =>
                {
                    var query = new PartsQueryDto { From = from, To = to };

                    if (!query.IsValid())
                    {
                        return Results.BadRequest(new
                        {
                            error = "'from' must be less than or equal to 'to'"
                        });
                    }

                    var result = await fileService.GetPartsAsync(sessionId, from, to, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetUploadParts").WithDescription("Get the parts of a multipart upload session.");

        group.MapPost("/uploads/{sessionId:guid}/parts",
            async (
                [FromRoute] Guid sessionId,
                [FromBody] IReadOnlyList<UploadPartDto> parts,
                IFileService fileService,
                CancellationToken cancellationToken
            ) =>
            {
                if (parts == null || parts.Count == 0)
                {
                    return Results.BadRequest(new
                    {
                        error = "Parts list cannot be null or empty."
                    });
                }

                var result = await fileService.ConfirmPartsAsync(sessionId, parts, cancellationToken);
                return Results.Ok(result);
            })
        .WithName("ConfirmUploadParts").WithDescription("Confirm the parts of a multipart upload session.");

        group.MapPost("/uploads/{sessionId:guid}/abort",
            async (
                [FromRoute] Guid sessionId,
                IFileService fileService,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await fileService.AbortUploadAsync(sessionId, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("AbortMultipartUpload").WithDescription("Abort a multipart upload session, discarding all uploaded parts.");

        return app;

    }
}
