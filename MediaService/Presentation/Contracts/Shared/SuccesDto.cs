namespace MediaService.Presentation.Contracts.Shared;

public sealed record SuccessDto
{
    public required string Message { get; init; }
}
