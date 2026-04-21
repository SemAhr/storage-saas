using System.ComponentModel.DataAnnotations;

namespace MediaService.Contracts.Files;

public sealed class PresignedRequestDto
{
    [Required]
    [MinLength(1)]
    public string FileName { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string MimeType { get; init; } = null!;

    [Range(1, long.MaxValue, ErrorMessage = "Size must be greater than 0.")]
    public long Size { get; init; } = 0;
};
