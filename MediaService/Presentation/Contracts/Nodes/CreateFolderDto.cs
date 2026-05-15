using System.ComponentModel.DataAnnotations;

namespace MediaService.Presentation.Contracts.Nodes;

public sealed class CreateFolderDto
{
    public Guid? ParentId { get; set; }

    [MinLength(1)]
    public required string Name { get; set; }
}
