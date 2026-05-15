using MediaService.Domain.Exceptions;
using MediaService.Domain.Nodes;
using MediaService.Presentation.Contracts.Files;
using MediaService.Presentation.Contracts.Nodes;
using MediaService.Presentation.Contracts.Shared;

namespace MediaService.Application.Nodes;

public sealed class NodesService(INodeRepository nodeRepository) : INodesService
{
    private readonly INodeRepository _nodeRepository = nodeRepository;

    public async Task<NodeDto> CreateFolderAsync(Guid? parentId, string name, CancellationToken cancellationToken = default)
    {
        var newNode = new NodeEntity
        {
            ParentId = parentId,
            Name = name
        };

        return await _nodeRepository.AddAsync(newNode, cancellationToken) is NodeEntity node
            ? new NodeDto
            {
                Id = node.Id,
                ParentId = node.ParentId?.ToString(),
                Name = node.Name,
                Type = node.Type
            }
            : throw new Exception("Failed to create folder.");
    }

    public async Task<NodeDto> GetByIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _nodeRepository.GetByIdAsync(nodeId, cancellationToken) is NodeEntity node
            ? new NodeDto
            {
                Id = node.Id,
                ParentId = node.ParentId?.ToString(),
                Name = node.Name,
                Type = node.Type,
                File = node.File is not null
                    ? new FileDto
                    {
                        MimeType = node.File.MimeType,
                        Size = node.File.Size,
                        StorageUrl = node.File.StorageUrl,
                    }
                    : null
            }
            : throw new NotFoundException("Node not found.");
    }

    public async Task<IReadOnlyList<NodeDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await _nodeRepository.GetChildrenAsync(parentId, cancellationToken) is IReadOnlyList<NodeEntity> nodes
            ? nodes.Select(node => new NodeDto
            {
                Id = node.Id,
                ParentId = node.ParentId?.ToString(),
                Name = node.Name,
                Type = node.Type,
                File = node.File is not null
                    ? new FileDto
                    {
                        MimeType = node.File.MimeType,
                        Size = node.File.Size,
                        StorageUrl = node.File.StorageUrl,
                    }
                    : null
            }).ToList()
            : throw new NotFoundException("Parent node not found.");
    }

    public async Task<SuccessDto> RenameAsync(Guid nodeId, string newName, CancellationToken cancellationToken = default)
    {
        return await _nodeRepository.RenameAsync(nodeId, newName, cancellationToken)
            ? new SuccessDto { Message = "Node renamed successfully." }
            : throw new NotFoundException("Node not found.");
    }

    public async Task<SuccessDto> MoveAsync(Guid nodeId, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        return await _nodeRepository.MoveAsync(nodeId, newParentId, cancellationToken)
            ? new SuccessDto { Message = "Node moved successfully." }
            : throw new NotFoundException("Node not found.");
    }

    public async Task<SuccessDto> DeleteAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _nodeRepository.SoftDeleteAsync(nodeId, cancellationToken)
            ? new SuccessDto { Message = "Node deleted successfully." }
            : throw new NotFoundException("Node not found.");
    }
}
