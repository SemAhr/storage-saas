using MediaService.Application.Shared.Nodes;
using MediaService.Data;
using MediaService.Domain.Entities;
using MediaService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Infrastructure.Persistence.Repositories.Nodes;

public sealed class NodeRepository(AppDbContext dbContext) : INodeRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<NodeEntity> AddAsync(NodeEntity node, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);
        cancellationToken.ThrowIfCancellationRequested();

        _dbContext.Nodes.Add(node);
        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);
        if (affectedRows == 0)
        {
            throw new Exception("Failed to add node to the database.");
        }

        return node;
    }

    public Task<NodeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Node id cannot be empty.", nameof(id));
        }

        return _dbContext.Nodes
            .AsNoTracking()
            .Include(node => node.File)
            .Include(node => node.Children
                .Where(childNode => childNode.DeletedAt == null)
                .OrderBy(childNode => childNode.Name))
            .ThenInclude(childNode => childNode.File)
            .FirstOrDefaultAsync(
                node => node.Id == id && node.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> RenameAsync(Guid id, string newName, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Node id cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(newName);

        var affectedRows = await _dbContext.Nodes
            .Where(node => node.Id == id && node.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(node => node.Name, newName)
                .SetProperty(node => node.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        return affectedRows > 0;
    }

    public async Task<bool> MoveAsync(Guid id, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Node id cannot be empty.", nameof(id));
        }

        var affectedRows = await _dbContext.Nodes
            .Where(node => node.Id == id && node.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(node => node.ParentId, newParentId)
                .SetProperty(node => node.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        return affectedRows > 0;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var node = await _dbContext.Nodes
            .FirstOrDefaultAsync(currentNode => currentNode.Id == id, cancellationToken);

        if (node is null)
        {
            return false;
        }

        node.DeletedAt = DateTime.UtcNow;
        node.UpdatedAt = DateTime.UtcNow;

        var success = await _dbContext.SaveChangesAsync(cancellationToken);
        if (success == 0)
        {
            throw new Exception("Failed to soft delete node in the database.");
        }

        return true;
    }
}
