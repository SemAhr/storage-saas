using MediaService.Domain.Enums;

namespace MediaService.Contracts.Media;

public sealed record MediaDto(
    Guid Id,
    string FileName,
    string MimeType,
    long Size,
    string Url,
    Status Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);
