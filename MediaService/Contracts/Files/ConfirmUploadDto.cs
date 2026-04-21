using MediaService.Shared.Validations;

namespace MediaService.Contracts.Files;

public sealed class ConfirmUploadDto
{
    [Guid]
    public string NodeId { get; init; } = null!;
};
