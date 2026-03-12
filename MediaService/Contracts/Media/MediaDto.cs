using MediaService.Domain.Enums;

namespace MediaService.Contracts.Media;

public sealed record MediaDto(
    Guid Id,
    Guid NodeId,
    string Name,
    string MimeType,
    long Size,
    string StoragePath,
    Status Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);
