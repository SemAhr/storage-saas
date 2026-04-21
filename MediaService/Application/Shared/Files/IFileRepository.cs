using MediaService.Domain.Entities;
using MediaService.Domain.Enums;

namespace MediaService.Application.Shared.Files;

public interface IFileRepository
{
    Task<FileEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FileEntity?> UpdateStatusAsync(Guid id, UploadStatus status, CancellationToken cancellationToken = default);
}
