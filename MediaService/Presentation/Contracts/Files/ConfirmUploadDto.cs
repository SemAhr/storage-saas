namespace MediaService.Presentation.Contracts.Files;

public sealed class ConfirmUploadDto
{
    public required Guid NodeId { get; init; }
};
