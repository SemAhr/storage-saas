using MediaService.Contracts.Media;

namespace MediaService.Application.Media;

public interface IMediaService
{
    Task<IReadOnlyList<MediaDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MediaDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(PresignedDto? presigned, string? error)> PresignedUploadAsync(PresignedUploadDto presignedUploadDto, CancellationToken cancellationToken = default);
    Task<(ConfirmUploadDto? confirmUpload, string? error)> ConfirmUploadAsync(ConfirmUploadDto confirmUploadDto, CancellationToken cancellationToken = default);

    Task<(MediaDto? media, string? error)>
}
