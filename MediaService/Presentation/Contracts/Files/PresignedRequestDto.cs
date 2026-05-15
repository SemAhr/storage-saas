using System.ComponentModel.DataAnnotations;

namespace MediaService.Presentation.Contracts.Files;

public sealed class PresignedRequestDto
{
    public Guid? ParentId { get; init; }

    [MinLength(1)]
    public required string FileName { get; init; }

    [MinLength(1)]
    public required string MimeType { get; init; }

    [Range(1, long.MaxValue, ErrorMessage = "Size must be greater than 0.")]
    public required long Size { get; init; }
};
