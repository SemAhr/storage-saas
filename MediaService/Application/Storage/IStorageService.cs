namespace MediaService.Application.Storage;

public interface IStorageService
{
    long CalculateBasePartSize(long fileSize, long defaultPartSize, long minimumPartSize, long maximumPartSize, int maximumPartsCount);
    string GenerateStorageKey(string fileName, Guid nodeId);
    Task<string> GenerateSingleUploadUrlAsync(string key, string mimeType, DateTime expiresAt);
    Task<string> GenerateMultipartUploadUrlAsync(string key, string mimeType, DateTime expiresAt);
    string GenerateDownloadUrl(string key, DateTime expiresAt);
    Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default);
}
