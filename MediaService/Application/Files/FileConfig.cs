namespace MediaService.Application.Files;

public sealed class FileConfig
{
    public const string SectionName = "File";

    public TimeSpan UploadExpiration { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan DownloadExpiration { get; init; } = TimeSpan.FromMinutes(15);
}
