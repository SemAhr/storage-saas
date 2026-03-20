using MediaService.Contracts.File;
using MediaService.Contracts.Shared;

namespace MediaService.Application.File;

public interface IFileService
{
    Task<PresignedResponseDto> PresignedUploadAsync(PresignedRequestDto presignedUploadDto, CancellationToken cancellationToken = default);
    Task<SuccessDto> ConfirmUploadAsync(ConfirmUploadDto confirmUploadDto, CancellationToken cancellationToken = default);
}
