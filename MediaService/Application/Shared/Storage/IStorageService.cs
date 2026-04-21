namespace MediaService.Application.Shared.Storage;

public interface IStorageService
{
    string GenerateS3Key(string fileName, Guid nodeId);
    Task<string> GenerateUploadUrlAsync(string key, string mimeType, CancellationToken cancellationToken = default);
    Task<string> GenerateDownloadUrlAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string key, CancellationToken cancellationToken = default);
}
