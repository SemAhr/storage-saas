namespace MediaService.Presentation.Contracts.Files;

public sealed class PresignedResponseDto
{
    public required Guid NodeId { get; init; }
    public required string UploadUrl { get; init; }
    public required string StorageKey { get; init; }
    public required DateTime ExpiresAt { get; init; }
};
