namespace MediaService.Application.Storage;

public interface IStorageService
{
    long CalculateBasePartSize(long fileSize, long defaultPartSize, long minimumPartSize, long maximumPartSize, int maximumPartsCount);
    string GenerateS3Key(string fileName, Guid nodeId);
    string GenerateUploadUrl(string key, string mimeType, DateTime expiresAt);
    string GenerateDownloadUrl(string key, DateTime expiresAt);
    Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default);
}
