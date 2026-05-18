using System.ComponentModel.DataAnnotations;

namespace MediaService.Presentation.Contracts.Files;

public sealed record UploadDto
{
    public Guid? ParentId { get; init; }

    [Required(ErrorMessage = "File name is required.")]
    [StringLength(255, ErrorMessage = "File name cannot exceed 255 characters.")]
    public required string FileName { get; init; }

    [Required(ErrorMessage = "Mime type is required.")]
    [StringLength(100, ErrorMessage = "Mime type cannot exceed 100 characters.")]
    public required string MimeType { get; init; }

    [Range(1, long.MaxValue, ErrorMessage = "Size must be greater than 0.")]
    public required long Size { get; init; }
}
