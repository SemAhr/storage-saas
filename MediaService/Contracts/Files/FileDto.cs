namespace MediaService.Contracts.Files;

public sealed record FileDto(
    Guid Id,
    Guid ParentId,
    string Name,
    string MimeType,
    long Size,
    string StoragePath,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);
