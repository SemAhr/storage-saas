namespace MediaService.Contracts.Media;

public sealed record PresignedDto(
    string UpladoUrl,
    string Key,
    Guid FileId,
    DateTime ExpiresAt
);
