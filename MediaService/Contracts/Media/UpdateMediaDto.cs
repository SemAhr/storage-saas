namespace MediaService.Contracts.Media;

public sealed record UpdateMediaDto(
    Guid? NodeId,
    string? Status
);
