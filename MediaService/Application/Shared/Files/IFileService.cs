using MediaService.Contracts.Files;
using MediaService.Contracts.Shared;

namespace MediaService.Application.Shared.Files;

public interface IFileService
{
    Task<PresignedResponseDto> PresignedUploadAsync(PresignedRequestDto presignedUploadDto, CancellationToken cancellationToken = default);
    Task<SuccessDto> ConfirmUploadAsync(ConfirmUploadDto confirmUploadDto, CancellationToken cancellationToken = default);
}
