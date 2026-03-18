namespace MediaService.Contracts.File;

public sealed class PresignedRequestDto
{
    public string FileName { get; init; } = null!;
    public string MimeType { get; init; } = null!;
    public long Size { get; init; }
};
