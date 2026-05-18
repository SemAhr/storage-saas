using MediaService.Application.Files;
using MediaService.Domain.Files;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Infrastructure.Persistence.Repositories;

public sealed class FileRepository(AppDbContext dbContext) : IFileRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<Guid> CreateSingleUploadSessionAsync(FileEntity file, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Node is null)
        {
            throw new ArgumentException("File node is required.", nameof(file));
        }

        return await _dbContext.Database
            .SqlQuery<Guid>($"""
                select create_single_upload_session(
                    p_node_id := {file.NodeId},
                    p_parent_id := {file.Node.ParentId},
                    p_name := {file.Node.Name},
                    p_mime_type := {file.MimeType},
                    p_size := {file.Size},
                    p_object_key := {file.ObjectKey},
                    p_expires_at := {expiresAt}
                ) as "Value"
                """)
            .SingleAsync(cancellationToken);
    }

    public async Task<Guid> CreateMultipartUploadSessionAsync(FileEntity file, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Node is null)
        {
            throw new ArgumentException("File node is required.", nameof(file));
        }

        return await _dbContext.Database
            .SqlQuery<Guid>($"""
                select create_multipart_upload_session(
                    p_node_id := {file.NodeId},
                    p_parent_id := {file.Node.ParentId},
                    p_name := {file.Node.Name},
                    p_mime_type := {file.MimeType},
                    p_size := {file.Size},
                    p_object_key := {file.ObjectKey},
                    p_expires_at := {expiresAt}
                ) as "Value"
                """)
            .SingleAsync(cancellationToken);
    }

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
