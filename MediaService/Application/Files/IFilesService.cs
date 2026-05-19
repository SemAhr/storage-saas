using MediaService.Presentation.Contracts.Files;
using MediaService.Presentation.Contracts.Shared;
using OneOf;

namespace MediaService.Application.Files;

public interface IFileService
{
    Task<OneOf<SingleUploadDto, MultipartUploadDto>> UploadAsync(UploadDto uploadDto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartUploadUrlDto>> GetPartsAsync(
        Guid sessionId,
        int from,
        int to,
        CancellationToken cancellationToken = default);

    Task<UploadedPartsResponseDto> ConfirmPartsAsync(
        Guid sessionId,
        IReadOnlyList<UploadPartDto> parts,
        CancellationToken cancellationToken = default);

    Task<SuccessDto> AbortUploadAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<SuccessDto> ConfirmUploadAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
