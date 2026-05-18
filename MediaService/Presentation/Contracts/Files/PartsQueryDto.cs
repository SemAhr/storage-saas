using System.ComponentModel.DataAnnotations;

namespace MediaService.Presentation.Contracts.Files;

public sealed record PartsQueryDto
{
    [Range(1, int.MaxValue, ErrorMessage = "'from' must be greater than 0")]
    public required int From { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "'to' must be greater than 0")]
    public required int To { get; init; }

    public bool IsValid() => From <= To;
}
