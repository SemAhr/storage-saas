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

    public async Task AttachMultipartUploadPartAsync(Guid sessionId, string storageUploadId, long partSize, int partsCount, CancellationToken cancellationToken = default)
    {
        await _dbContext.Database
            .ExecuteSqlInterpolatedAsync($"""
                select attach_multipart_upload_part(
                    p_session_id := {sessionId},
                    p_storage_upload_id := {storageUploadId},
                    p_part_size := {partSize},
                    p_parts_count := {partsCount}
                )
                """, cancellationToken);
    }

    public async Task FinishMultipartUploadAsync(Guid sessionId, UploadStatus status, string reason, CancellationToken cancellationToken = default)
    {
        await _dbContext.Database
            .ExecuteSqlInterpolatedAsync($"""
                select finish_multipart_upload(
                    p_session_id := {sessionId},
                    p_status := {status},
                    p_reason := {reason}
                )
                """, cancellationToken);
    }

    public async Task<MultipartUploadSessionDetailsDto> GetMultipartUploadSessionDetails(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database
            .SqlQuery<MultipartUploadSessionDetailsDto>($"""
                    select
                    sessions.id as "SessionId",
                    files.node_id as "NodeId",
                    files.object_key as "ObjectKey",
                    files.mime_type as "MimeType",
                    files.size as "Size",
                    multipart.storage_upload_id as "StorageUploadId",
                    multipart.part_size as "PartSize",
                    multipart.parts_count as "PartsCount",
                    sessions.expires_at as "ExpiresAt",
                    sessions.status as "SessionStatus",
                    files.status as "FileStatus"
                    from file_upload_sessions as sessions
                    join files
                        on files.node_id = sessions.node_id
                    join multipart_uploads as multipart
                        on multipart.session_id = sessions.id
                    where sessions.id = {sessionId}
                    and sessions.upload_mode = 'multipart';
                    """)
            .SingleOrDefaultAsync(cancellationToken)
                ?? throw new Exception($"Multipart upload session with id '{sessionId}' not found.");
    }

    public async Task<IReadOnlyList<PartForConfirmDto>> GetPartsForConfirmAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database
            .SqlQuery<PartForConfirmDto>($"""
                    select
                        part_number as "PartNumber",
                        etag as "Etag"
                    from multipart_upload_parts
                    where session_id = {sessionId}
                    order by part_number asc;
                    """)
            .ToListAsync(cancellationToken);
    }

    public async Task<UploadForAbortDto> GetUploadForAbortAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database
            .SqlQuery<UploadForAbortDto>($"""
                    select
                        sessions.id as "SessionId",
                        files.node_id as "NodeId",
                        files.object_key as "ObjectKey",
                        multipart.storage_upload_id,
                    from file_upload_sessions as sessions
                    join files
                        on files.node_id = sessions.node_id
                    left join multipart_uploads as multipart
                        on multipart.session_id = sessions.id
                    where sessions.id = {sessionId}
                    and sessions.upload_mode = 'multipart';
                    """)
            .SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Upload session with id '{sessionId}' not found.");
    }


    // public async Task<FileEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    // {
    //     return await _dbContext.Files
    //         .AsNoTracking()
    //         .FirstOrDefaultAsync(
    //             file =>
    //                 file.NodeId == id &&
    //                 file.Node != null &&
    //                 file.Node.DeletedAt == null,
    //             cancellationToken);
    // }

    // public async Task<bool> UpdateStatusAsync(Guid id, UploadStatus status, CancellationToken cancellationToken = default)
    // {
    //     if (id == Guid.Empty)
    //     {
    //         throw new ArgumentException("Node id cannot be empty.", nameof(id));
    //     }

    //     var affectedRows = await _dbContext.Files
    //         .Where(file => file.NodeId == id)
    //         .ExecuteUpdateAsync(setters => setters
    //             .SetProperty(file => file.Status, status)
    //             .SetProperty(file => file.UpdatedAt, DateTime.UtcNow),
    //             cancellationToken);

    //     return affectedRows > 0;
    // }
}
