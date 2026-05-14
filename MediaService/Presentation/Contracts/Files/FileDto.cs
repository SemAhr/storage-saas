namespace MediaService.Presentation.Contracts.Files;

public sealed class FileDto
{
    public string MimeType { get; init; } = null!;
    public long Size { get; init; }
    public string StorageUrl { get; init; } = null!;
};
