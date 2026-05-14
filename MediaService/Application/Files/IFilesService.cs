using MediaService.Presentation.Contracts.Files;
using MediaService.Presentation.Contracts.Shared;

namespace MediaService.Application.Files;

public interface IFileService
{
    Task<PresignedResponseDto> PresignedUploadAsync(PresignedRequestDto presignedUploadDto, CancellationToken cancellationToken = default);
    Task<SuccessDto> ConfirmUploadAsync(ConfirmUploadDto confirmUploadDto, CancellationToken cancellationToken = default);
}
