using MediaService.Domain.Nodes;
using MediaService.Presentation.Contracts.Files;

namespace MediaService.Presentation.Contracts.Nodes;

/// <summary>
/// Response DTO for a node (file or folder)
/// </summary>
public sealed record NodeDto
{
    public required Guid Id { get; init; }
    public string? ParentId { get; init; }
    public required string Name { get; init; }
    public required NodeType Type { get; init; }
    public FileDto? File { get; init; }
}
