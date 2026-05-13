using MediaService.Domain.Entities;

namespace MediaService.Application.Shared.Nodes;

public interface INodeRepository
{
    Task<NodeEntity> AddAsync(NodeEntity node, CancellationToken cancellationToken = default);
    Task<NodeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NodeEntity?> UpdateAsync(Guid id, NodeEntity node, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
