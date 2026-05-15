using MediaService.Domain.Nodes;

namespace MediaService.Domain.Files;

public sealed class FileEntity
{
    public Guid NodeId { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? StorageUrl { get; set; }
    public UploadStatus Status { get; set; }
    public DateTime? UploadExpiresAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public NodeEntity? Node { get; set; }
}
