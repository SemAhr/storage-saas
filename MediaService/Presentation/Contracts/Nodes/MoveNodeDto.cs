namespace MediaService.Presentation.Contracts.Nodes;

public sealed class MoveNodeDto
{
    public required Guid ParentId { get; init; }
}
