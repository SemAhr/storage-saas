namespace MediaService.Contracts.Node;

public sealed class UpdateNodeDto
{
    public Guid? ParentId { get; init; }
    public string? Name { get; init; }
};
