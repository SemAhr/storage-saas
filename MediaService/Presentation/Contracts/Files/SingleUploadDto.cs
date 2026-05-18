using MediaService.Domain.Files;

namespace MediaService.Presentation.Contracts.Files;

public sealed record SingleUploadDto
{
    public required Guid NodeId { get; init; }
    public required Guid SessionId { get; init; }
    public required UploadMode UploadMode { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string UploadUrl { get; init; }
}
