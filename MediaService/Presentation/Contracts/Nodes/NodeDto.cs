using MediaService.Presentation.Contracts.Files;

namespace MediaService.Presentation.Contracts.Nodes;

public sealed class NodeDto
{
    public string Id { get; init; } = null!;
    public string? ParentId { get; init; }
    public string Name { get; init; } = null!;
    public string Type { get; init; } = null!;
    public FileDto? File { get; init; }
};
