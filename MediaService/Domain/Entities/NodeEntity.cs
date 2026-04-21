using MediaService.Domain.Enums;

namespace MediaService.Domain.Entities;

public sealed class NodeEntity
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public NodeType? Type { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public NodeEntity? Parent { get; set; }
    public ICollection<NodeEntity>? Children { get; set; }
    public FileEntity? File { get; set; }
}
