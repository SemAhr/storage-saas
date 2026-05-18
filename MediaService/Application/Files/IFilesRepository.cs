using MediaService.Domain.Files;

namespace MediaService.Application.Files;

public interface IFileRepository
{
    Task<Guid> CreateSingleUploadSessionAsync(FileEntity file, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task<Guid> CreateMultipartUploadSessionAsync(FileEntity file, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task AttachMultipartUploadPartAsync(Guid sessionId, string storageUploadId, long partSize, int partsCount, CancellationToken cancellationToken = default);
    Task FinishMultipartUploadAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<FileEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(Guid id, UploadStatus status, CancellationToken cancellationToken = default);
}
