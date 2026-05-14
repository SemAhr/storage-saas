namespace MediaService.Application.Storage;

public interface IStorageService
{
    string GenerateS3Key(string fileName, Guid nodeId);
    UrlGenerated GenerateUploadUrl(string key, string mimeType);
    UrlGenerated GenerateDownloadUrl(string key);
    Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default);
}
