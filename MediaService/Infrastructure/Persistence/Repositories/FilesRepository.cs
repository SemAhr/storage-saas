using MediaService.Application.Files;
using MediaService.Domain.Files;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Infrastructure.Persistence.Repositories;

public sealed class FileRepository(AppDbContext dbContext) : IFileRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<FileEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(
                file =>
                    file.NodeId == id &&
                    file.Node != null &&
                    file.Node.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, UploadStatus status, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Node id cannot be empty.", nameof(id));
        }

        var affectedRows = await _dbContext.Files
            .Where(file => file.NodeId == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(file => file.Status, status)
                .SetProperty(file => file.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        return affectedRows > 0;
    }
}
