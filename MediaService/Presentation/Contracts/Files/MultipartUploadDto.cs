using MediaService.Domain.Files;

namespace MediaService.Presentation.Contracts.Files;

public sealed record MultipartUploadDto
{
    public required Guid NodeId { get; init; }
    public required Guid SessionId { get; init; }
    public required UploadMode UploadMode { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required long PartSize { get; init; }
    public required int PartsCount { get; init; }
}
