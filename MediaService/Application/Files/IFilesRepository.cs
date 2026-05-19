using MediaService.Domain.Files;

namespace MediaService.Application.Files;

public interface IFileRepository
{
    Task<Guid> CreateSingleUploadSessionAsync(
        FileEntity file,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateMultipartUploadSessionAsync(
        FileEntity file,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task AttachMultipartUploadPartAsync(
        Guid sessionId,
        string storageUploadId,
        long partSize,
        int partsCount,
        CancellationToken cancellationToken = default);

    Task FinishMultipartUploadAsync(
        Guid sessionId,
        UploadStatus status,
        string reason,
        CancellationToken cancellationToken = default);

    Task<MultipartUploadSessionDetailsDto> GetMultipartUploadSessionDetails(Guid sessionId, CancellationToken cancellationToken = default);
    Task<UploadForAbortDto> GetUploadForAbortAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartForConfirmDto>> GetPartsForConfirmAsync(Guid sessionId, CancellationToken cancellationToken = default);

    // Task<FileEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Task<bool> UpdateStatusAsync(Guid id, UploadStatus status, CancellationToken cancellationToken = default);
}

public sealed record MultipartUploadSessionDetailsDto(
    Guid SessionId,
    Guid NodeId,
    string ObjectKey,
    string MimeType,
    long Size,
    string StorageUploadId,
    long PartSize,
    int PartsCount,
    DateTime ExpiresAt,
    UploadStatus SessionStatus,
    UploadStatus FileStatus);

public sealed record PartForConfirmDto(
    int PartNumber,
    string Etag);

public sealed record UploadForAbortDto(
    Guid SessionId,
    Guid NodeId,
    string ObjectKey,
    string StorageUploadId);
