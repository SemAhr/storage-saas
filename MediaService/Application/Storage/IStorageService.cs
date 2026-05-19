using MediaService.Presentation.Contracts.Files;

namespace MediaService.Application.Storage;

public interface IStorageService
{
    long CalculateBasePartSize(
        long fileSize,
        long defaultPartSize,
        long minimumPartSize,
        long maximumPartSize,
        int maximumPartsCount);

    string GenerateStorageKey(string fileName, Guid nodeId);
    string GenerateSingleUploadUrl(string key, string mimeType, DateTime expiresAt);

    Task<string> GenerateMultipartUploadAsync(string key, string mimeType, CancellationToken cancellationToken = default);
    IReadOnlyList<PartUploadUrlDto> GenerateMultipartUploadUrls(
        string key,
        string uploadId,
        IEnumerable<int> partNumbers,
        DateTime expiresAt);

    Task<bool> AbortMultipartUploadAsync(string key, string uploadId, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default);

    string GenerateDownloadUrl(string key, DateTime expiresAt);
}
