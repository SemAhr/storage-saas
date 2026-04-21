namespace MediaService.Contracts.Nodes;

public sealed class UpdateNodeDto
{
    public string? ParentId { get; init; } = null!;
    public string? Name { get; init; } = null!;
};
