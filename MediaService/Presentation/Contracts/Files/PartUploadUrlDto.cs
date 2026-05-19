namespace MediaService.Presentation.Contracts.Files;

public sealed record PartUploadUrlDto
{
    public required int PartNumber { get; init; }
    public required string UploadUrl { get; init; }
}
