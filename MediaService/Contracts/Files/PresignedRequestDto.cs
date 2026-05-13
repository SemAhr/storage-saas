using System.ComponentModel.DataAnnotations;
using MediaService.Shared.Validations;

namespace MediaService.Contracts.Files;

public sealed class PresignedRequestDto
{
    [Guid]
    public string ParentId { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string FileName { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string MimeType { get; init; } = null!;

    [Range(1, long.MaxValue, ErrorMessage = "Size must be greater than 0.")]
    public long Size { get; init; } = 0;
};
