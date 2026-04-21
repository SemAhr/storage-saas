using MediaService.Contracts.Nodes;
using MediaService.Domain.Entities;

namespace MediaService.Application.Shared.Nodes;

public interface INodeRepository
{
    Task<NodeEntity> AddAsync(NodeDto node, CancellationToken cancellationToken = default);
    Task<NodeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NodeEntity?> UpdateAsync(Guid id, UpdateNodeDto node, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
