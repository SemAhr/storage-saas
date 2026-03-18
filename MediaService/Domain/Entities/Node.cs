using MediaService.Domain.Enums;

namespace MediaService.Domain.Entities;

public sealed class Node
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public NodeType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; } = null;

    public Node? Parent { get; set; }
    public ICollection<Node> Children { get; set; } = [];
    public FileEntry? File { get; set; }
}
