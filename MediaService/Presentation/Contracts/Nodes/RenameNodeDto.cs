using System.ComponentModel.DataAnnotations;

namespace MediaService.Presentation.Contracts.Nodes;

public sealed record RenameNodeDto
{
    [Required(ErrorMessage = "New name is required.")]
    [StringLength(255, ErrorMessage = "New name cannot exceed 255 characters.")]
    public required string NewName { get; init; }
}
