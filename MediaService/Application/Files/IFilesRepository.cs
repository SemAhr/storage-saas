using MediaService.Domain.Files;

namespace MediaService.Application.Files;

public interface IFileRepository
{
    Task<FileEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(Guid id, UploadStatus status, CancellationToken cancellationToken = default);
}
