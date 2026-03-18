using MediaService.Contracts.File;

namespace MediaService.Contracts.Node;

public sealed class NodeDto
{
    public Guid? ParentId { get; init; }
    public string Name { get; init; } = null!;
    public string Type { get; init; } = null!;
    public FileDto? File { get; init; }
};
