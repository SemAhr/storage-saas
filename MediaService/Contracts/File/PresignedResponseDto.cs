namespace MediaService.Contracts.File;

public sealed class PresignedResponseDto
{
    public Guid NodeId { get; init; }
    public string UploadUrl { get; init; } = null!;
    public string Key { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
};
