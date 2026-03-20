using System.ComponentModel.DataAnnotations;
using MediaService.Application.File;
using MediaService.Contracts.File;
using Microsoft.AspNetCore.Mvc;

namespace MediaService.Api.Endpoints;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/files").WithTags("Files");

        group.MapPost("/presigned-upload", async ([FromBody] PresignedRequestDto presignedRequestDto, IFileService fileService, CancellationToken cancellationToken) =>
        {
            var result = await fileService.PresignedUploadAsync(presignedRequestDto, cancellationToken);
            return Results.Ok(result);
        }).WithName("PresignedUpload").WithDescription("Get a presigned URL for uploading a file.");

        group.MapPost("/confirm-upload", async (ConfirmUploadDto confirmUploadDto, IFileService fileService, CancellationToken cancellationToken) =>
            {
                var result = await fileService.ConfirmUploadAsync(confirmUploadDto, cancellationToken);
                return Results.Ok(result);
            }).WithName("ConfirmUpload").WithDescription("Confirm that a file has been uploaded successfully.");

        return app;
    }
}
