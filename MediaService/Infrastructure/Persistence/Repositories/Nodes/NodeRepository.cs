using MediaService.Application.Shared.Nodes;
using MediaService.Contracts.Nodes;
using MediaService.Data;
using MediaService.Domain.Entities;
using MediaService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Infrastructure.Persistence.Repositories.Nodes;

public sealed class NodeRepository(AppDbContext dbContext) : INodeRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<NodeEntity> AddAsync(NodeDto nodeDto, CancellationToken cancellationToken = default)
    {
        var node = new NodeEntity
        {
            Name = nodeDto.Name,
            Type = !string.IsNullOrEmpty(nodeDto.Type) ? Enum.Parse<NodeType>(nodeDto.Type, true) : null,
            ParentId = !string.IsNullOrEmpty(nodeDto.ParentId) ? Guid.Parse(nodeDto.ParentId) : null,
            File = nodeDto.File != null ? new FileEntity
            {
                MimeType = nodeDto.File.MimeType,
                Size = nodeDto.File.Size
            } : null
        };

        var savedNode = await _dbContext.Nodes.AddAsync(node, cancellationToken);
        var success = await _dbContext.SaveChangesAsync(cancellationToken);
        if (success == 0)
        {
            throw new Exception("Failed to add node to the database.");
        }

        return savedNode.Entity;
    }

    public Task<NodeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Nodes
            // .Include(node => node.File)
            .Include(node => node.Children)
            .FirstOrDefaultAsync(node => node.Id == id, cancellationToken);
    }

    public async Task<NodeEntity?> UpdateAsync(Guid id, UpdateNodeDto node, CancellationToken cancellationToken = default)
    {
        var existingNode = await _dbContext.Nodes
            .FirstOrDefaultAsync(currentNode => currentNode.Id == id, cancellationToken);

        if (existingNode is null)
        {
            return null;
        }

        existingNode.Name = node.Name ?? existingNode.Name;
        existingNode.UpdatedAt = DateTime.UtcNow;

        var success = await _dbContext.SaveChangesAsync(cancellationToken);
        if (success == 0)
        {
            throw new Exception("Failed to update node in the database.");
        }

        return existingNode;
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
