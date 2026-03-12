using MediaService.Contracts.Media;

namespace MediaService.Application.Media;

public interface IMediaService
{
    Task<IReadOnlyList<MediaDto>> GetAllAsync(CancellationToken cancellationToken = default);
    // Task<MediaDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Task<PresignedDto> PresignedUploadAsync(PresignedRquestDto presignedUploadDto, CancellationToken cancellationToken = default);
    // Task<ConfirmUploadDto> ConfirmUploadAsync(ConfirmUploadDto confirmUploadDto, CancellationToken cancellationToken = default);
    // Task<MediaDto> UpdateAsync(MediaDto mediaDto, CancellationToken cancellationToken = default);
    // Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
