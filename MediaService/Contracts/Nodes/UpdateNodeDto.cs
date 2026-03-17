namespace MediaService.Contracts.Nodes;

public sealed record UpdateNodeDto(
    Guid? ParentId,
    string? Name
);
