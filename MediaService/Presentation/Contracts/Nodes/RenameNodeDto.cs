using System.ComponentModel.DataAnnotations;

namespace MediaService.Presentation.Contracts.Nodes;

public sealed class RenameNodeDto
{
    [MinLength(1)]
    public required string NewName { get; init; }
}
