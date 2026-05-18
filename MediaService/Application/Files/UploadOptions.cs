namespace MediaService.Application.Files;


public sealed class UploadOptions
{
    private const long Mib = 1024 * 1024;
    private const long Gib = 1024 * Mib;

    public const string SectionName = "Upload";

    public TimeSpan UploadExpiration { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan DownloadExpiration { get; init; } = TimeSpan.FromMinutes(15);

    public int MaxPartsCount { get; init; } = 10000;

    public long MaxFileSizeGib { get; init; } = 100;
    public long SingleUploadMaxSizeMib { get; init; } = 64;
    public long DefaultPartSizeMib { get; init; } = 10;
    public long MinPartSizeMib { get; init; } = 5;
    public long MaxPartSizeMib { get; init; } = 128;

    public long MaxFileSize => MaxFileSizeGib * Gib;
    public long SingleUploadMaxSize => SingleUploadMaxSizeMib * Mib;
    public long DefaultPartSize => DefaultPartSizeMib * Gib;
    public long MinPartSize => MinPartSizeMib * Mib;
    public long MaxPartSize => MaxPartSizeMib * Gib;
}
