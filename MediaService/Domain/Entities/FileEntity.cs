using MediaService.Domain.Enums;

namespace MediaService.Domain.Entities;

public class FileEntity
{
    public Guid NodeId { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public UploadStatus Status { get; set; }
    public DateTime? UploadExpiresAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public NodeEntity? Node { get; set; }
}
