namespace MediaService.Contracts.Media;

public sealed record PresignedDto(
    string UpladoPath,
    string Key,
    Guid FileId,
    DateTime ExpiresAt
);
