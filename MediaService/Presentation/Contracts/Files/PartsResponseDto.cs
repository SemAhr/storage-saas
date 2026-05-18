namespace MediaService.Presentation.Contracts.Files;

public sealed record PartsResponseDto
{
    public required int PartNumber { get; init; }
    public required string UploadUrl { get; init; }
}
