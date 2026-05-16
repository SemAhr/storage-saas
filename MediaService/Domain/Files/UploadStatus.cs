namespace MediaService.Domain.Files;

public enum UploadStatus
{
    Pending,
    Uploading,
    Completed,
    Failed,
    Canceled,
    Expired
}
