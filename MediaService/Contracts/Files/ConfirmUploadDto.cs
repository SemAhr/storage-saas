using System.ComponentModel.DataAnnotations;
using MediaService.Shared.Validations;

namespace MediaService.Contracts.Files;

public sealed class ConfirmUploadDto
{
    [Required]
    [Guid]
    public string NodeId { get; init; } = null!;
};
