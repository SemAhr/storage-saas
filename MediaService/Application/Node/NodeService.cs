using MediaService.Contracts.File;
using MediaService.Contracts.Node;
using MediaService.Data;

namespace MediaService.Application.Node;

public sealed class NodeService(AppDbContext dbContext) : INodeService
{
    private readonly AppDbContext _dbContext = dbContext;

    public Task<NodeDto> CreateNodeAsync(NodeDto nodeDto, CancellationToken cancellationToken = default)
    {
        return new Task<NodeDto>(() => nodeDto);
    }

    public Task<NodeDto> GetNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return new Task<NodeDto>(() => new NodeDto
        {
            ParentId = Guid.NewGuid(),
            Name = "Example Node",
            Type = "Folder",
            File = new FileDto
            {
                Name = "file.pdf",
                MimeType = "application/pdf",
                Size = 1024,
                StorageUrl = "https://example.com/file.pdf"
            }
        });
    }

    public Task<IEnumerable<NodeDto>> GetChildNodesAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return new Task<IEnumerable<NodeDto>>(() =>
        [
            new NodeDto
            {
                ParentId = parentId,
                Name = "Child Node 1",
                Type = "File",
                File = new FileDto
                {
                    Name = "file1.pdf",
                    MimeType = "application/pdf",
                    Size = 2048,
                    StorageUrl = "https://example.com/file1.pdf"
                }
            },
            new NodeDto
            {
                ParentId = parentId,
                Name = "Child Node 2",
                Type = "Folder"
            }
        ]);
    }

    public Task<NodeDto> UpdateNodeAsync(Guid nodeId, UpdateNodeDto updateNodeDto, CancellationToken cancellationToken = default)
    {
        return new Task<NodeDto>(() => new NodeDto
        {
            ParentId = updateNodeDto.ParentId,
            Name = updateNodeDto.Name ?? "Updated Node",
            Type = "Folder",
            File = new FileDto
            {
                Name = "updated-file.pdf",
                MimeType = "application/pdf",
                Size = 4096,
                StorageUrl = "https://example.com/updated-file.pdf"
            }
        });
    }

    public Task<bool> DeleteNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return new Task<bool>(() => true);
    }
}
