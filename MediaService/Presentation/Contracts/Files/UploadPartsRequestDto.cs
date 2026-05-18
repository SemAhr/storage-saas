using System.ComponentModel.DataAnnotations;

namespace MediaService.Presentation.Contracts.Files;

public sealed record UploadPartsRequestDto
{
    [MinLength(1, ErrorMessage = "At least one part is required.")]
    [MaxLength(1000, ErrorMessage = "Number of parts cannot exceed 1000.")]
    public required IReadOnlyList<UploadPartDto> Parts { get; init; }
}

public sealed record UploadPartDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Part number must be greater than 0.")]
    public required int PartNumber { get; init; }

    [Required(ErrorMessage = "ETag is required.")]
    [StringLength(255, ErrorMessage = "ETag cannot exceed 255 characters.")]
    public required string Etag { get; init; }

    [Range(1, long.MaxValue, ErrorMessage = "Size must be greater than 0.")]
    public required long Size { get; init; }
}
