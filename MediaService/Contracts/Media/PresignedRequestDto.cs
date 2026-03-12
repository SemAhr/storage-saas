namespace MediaService.Contracts.Media;

public sealed record PresignedRquestDto(
    string FileName,
    string MimeType,
    long Size
);
