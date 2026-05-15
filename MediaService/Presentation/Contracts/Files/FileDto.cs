namespace MediaService.Presentation.Contracts.Files;

public sealed class FileDto
{
    public required string MimeType { get; init; }
    public required long Size { get; init; }
    public required string StorageUrl { get; init; }
};
