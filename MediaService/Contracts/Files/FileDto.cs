namespace MediaService.Contracts.Files;

public sealed class FileDto
{
    public string Name { get; init; } = null!;
    public string MimeType { get; init; } = null!;
    public long Size { get; init; }
    public string StorageUrl { get; init; } = null!;
};
