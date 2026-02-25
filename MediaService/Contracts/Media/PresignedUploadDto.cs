namespace MediaService.Contracts.Media;

public sealed record PresignedUploadDto(
    string FileName,
    string MimeType,
    long Size
);
