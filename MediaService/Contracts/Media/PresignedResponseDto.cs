namespace MediaService.Contracts.Media;

public sealed record PresignedResponseDto(
    string UpladoPath,
    string Key,
    Guid FileId,
    DateTime ExpiresAt
);
