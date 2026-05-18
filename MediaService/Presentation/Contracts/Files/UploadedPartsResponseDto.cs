namespace MediaService.Presentation.Contracts.Files;

public sealed record UploadedPartsResponseDto
{
    public required Guid SessionId { get; init; }
    public required IReadOnlyList<int> AcceptedPartNumbers { get; init; }
    public required IReadOnlyList<RejectedPartDto> Rejected { get; init; }
    public required UploadProgressDto Progress { get; init; }
}

public sealed record RejectedPartDto
{
    public required int PartNumber { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
}

public sealed record UploadProgressDto
{
    public required int UploadedParts { get; init; }
    public required int ExpectedParts { get; init; }
    public required long UploadedSize { get; init; }
    public required long ExpectedSize { get; init; }
}
