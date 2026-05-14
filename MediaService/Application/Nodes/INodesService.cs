using MediaService.Presentation.Contracts.Nodes;
using MediaService.Presentation.Contracts.Shared;

namespace MediaService.Application.Nodes;

public interface INodeService
{
    Task<NodeDto> GetByIdAsync(Guid nodeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NodeDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<SuccessDto> RenameAsync(Guid id, string newName, CancellationToken cancellationToken = default);
    Task<SuccessDto> MoveAsync(Guid id, Guid? newParentId, CancellationToken cancellationToken = default);
    Task<SuccessDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
