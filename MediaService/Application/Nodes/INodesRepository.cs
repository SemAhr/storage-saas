using MediaService.Domain.Nodes;

namespace MediaService.Application.Nodes;

public interface INodeRepository
{
    Task<NodeEntity> AddAsync(NodeEntity node, CancellationToken cancellationToken = default);
    Task<NodeEntity?> GetByIdAsync(Guid? id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NodeEntity>> GetChildrenAsync(Guid? parentId, CancellationToken cancellationToken = default);
    Task<bool> RenameAsync(Guid id, string newName, CancellationToken cancellationToken = default);
    Task<bool> MoveAsync(Guid id, Guid? newParentId, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
