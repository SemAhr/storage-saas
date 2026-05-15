using MediaService.Application.Nodes;
using MediaService.Presentation.Contracts.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace MediaService.Presentation.Endpoints.Nodes;

public static class NodesEndpoints
{
    public static IEndpointRouteBuilder MapNodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/nodes").WithTags("Nodes");

        group.MapPost("/folders",
            async ([FromBody] CreateFolderDto createFolderDto, INodesService nodesService, CancellationToken cancellationToken) =>
                {
                    var result = await nodesService.CreateFolderAsync(createFolderDto.ParentId, createFolderDto.Name, cancellationToken);
                    return Results.Created($"/nodes/{result.Id}", result);
                })
            .WithName("CreateFolder").WithDescription("Create a new folder under the specified parent node.");

        group.MapGet("/{id:guid}",
            async (Guid id, INodesService nodesService, CancellationToken cancellationToken) =>
                {
                    var result = await nodesService.GetByIdAsync(id, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetNodeById").WithDescription("Get a node by its ID.");

        group.MapGet("/{parentId:guid}/children",
            async (Guid parentId, INodesService nodesService, CancellationToken cancellationToken) =>
                {
                    var result = await nodesService.GetChildrenAsync(parentId, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetNodeChildren").WithDescription("Get the children of a node.");

        group.MapPatch("/{id:guid}/rename",
            async (Guid id, [FromBody] RenameNodeDto renameNodeDto, INodesService nodesService, CancellationToken cancellationToken) =>
                {
                    var result = await nodesService.RenameAsync(id, renameNodeDto.NewName, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("RenameNode").WithDescription("Rename a node.");

        group.MapPatch("/{id:guid}/move",
            async (Guid id, [FromBody] MoveNodeDto moveNodeDto, INodesService nodesService, CancellationToken cancellationToken) =>
                {
                    var result = await nodesService.MoveAsync(id, moveNodeDto.ParentId, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("MoveNode").WithDescription("Move a node to a new parent.");

        group.MapDelete("/{id:guid}",
            async (Guid id, INodesService nodesService, CancellationToken cancellationToken) =>
                {
                    var result = await nodesService.DeleteAsync(id, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("DeleteNode").WithDescription("Delete a node.");

        return app;
    }
}
