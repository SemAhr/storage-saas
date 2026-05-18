namespace MediaService.Presentation.Contracts.Nodes;

public sealed record MoveNodeDto
{
    public required Guid ParentId { get; init; }
}
