using MediaService.Domain.Enums;

namespace MediaService.Domain.Entities;

public sealed class File
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NodeId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public Status Status { get; set; } = Status.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; } = null;
}
