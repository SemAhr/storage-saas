using System.ComponentModel.DataAnnotations;

namespace MediaService.Presentation.Contracts.Nodes;

public sealed record CreateFolderDto
{
    public Guid? ParentId { get; init; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters.")]
    public required string Name { get; init; }
}
