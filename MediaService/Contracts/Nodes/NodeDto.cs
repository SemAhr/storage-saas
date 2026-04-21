using MediaService.Contracts.Files;

namespace MediaService.Contracts.Nodes;

public sealed class NodeDto
{
    public string? ParentId { get; init; }
    public string Name { get; init; } = null!;
    public string Type { get; init; } = null!;
    public string Status { get; init; } = null!;
    public FileDto? File { get; init; }
};
