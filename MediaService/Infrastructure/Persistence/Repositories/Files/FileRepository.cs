using MediaService.Application.Shared.Files;
using MediaService.Data;
using MediaService.Domain.Entities;
using MediaService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Infrastructure.Persistence.Repositories.Files;

public sealed class FileRepository(AppDbContext dbContext) : IFileRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public Task<FileEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Files.FirstOrDefaultAsync(file => file.NodeId == id, cancellationToken);
    }

    public async Task<FileEntity?> UpdateStatusAsync(Guid id, UploadStatus status, CancellationToken cancellationToken = default)
    {
        var existingFile = await _dbContext.Files
            .FirstOrDefaultAsync(file => file.NodeId == id, cancellationToken);

        if (existingFile is null)
        {
            return null;
        }

        existingFile.Status = status;
        existingFile.UpdatedAt = DateTime.UtcNow;

        var success = await _dbContext.SaveChangesAsync(cancellationToken);
        if (success == 0)
        {
            throw new Exception("Failed to update file status in the database.");
        }

        return existingFile;
    }
}
