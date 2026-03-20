using System.ComponentModel.DataAnnotations;

namespace MediaService.Contracts.File;

public sealed class PresignedRequestDto
{
    [Required]
    public string FileName { get; init; } = null!;

    [Required]
    public string MimeType { get; init; } = null!;

    [Required]
    public long Size { get; init; }
};
