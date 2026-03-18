namespace MediaService.Domain.Entities;

public sealed class FileEntry
{
    public Guid NodeId { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Node Node { get; set; } = null!;
}
