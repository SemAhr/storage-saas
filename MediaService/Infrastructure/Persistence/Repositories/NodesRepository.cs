using MediaService.Application.Nodes;
using MediaService.Domain.Nodes;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Infrastructure.Persistence.Repositories;

public sealed class NodeRepository(AppDbContext dbContext) : INodeRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<NodeEntity> AddAsync(NodeEntity node, CancellationToken cancellationToken)
    {
        _dbContext.Nodes.Add(node);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return node;
    }

    public async Task<NodeEntity?> GetByIdAsync(Guid? id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Node id cannot be empty.", nameof(id));
        }

        return await _dbContext.Nodes
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

    public async Task<IReadOnlyList<NodeEntity>> GetChildrenAsync(Guid? parentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Nodes
            .AsNoTracking()
            .Where(node => node.ParentId == parentId && node.DeletedAt == null)
            .Include(node => node.File)
            .OrderBy(node => node.Name)
            .ToListAsync(cancellationToken);
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
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Node id cannot be empty.", nameof(id));
        }

        var utcNow = DateTime.UtcNow;

        var affectedRows = await _dbContext.Nodes
            .Where(node => node.Id == id && node.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(node => node.DeletedAt, utcNow)
                .SetProperty(node => node.UpdatedAt, utcNow),
                cancellationToken);

        return affectedRows > 0;
    }
}
