using MediaService.Shared.Validations;

namespace MediaService.Contracts.File;

public sealed class ConfirmUploadDto
{
    [Guid]
    public string NodeId { get; init; } = null!;
};
