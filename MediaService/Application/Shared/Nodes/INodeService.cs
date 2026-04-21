using MediaService.Contracts.Nodes;

namespace MediaService.Application.Shared.Nodes;

public interface INodeService
{
    Task<NodeDto> CreateNodeAsync(NodeDto nodeDto, CancellationToken cancellationToken = default);
    Task<NodeDto> GetNodeAsync(Guid nodeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<NodeDto>> GetChildNodesAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<NodeDto> UpdateNodeAsync(Guid nodeId, UpdateNodeDto updateNodeDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteNodeAsync(Guid nodeId, CancellationToken cancellationToken = default);
}
