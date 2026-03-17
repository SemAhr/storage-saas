namespace MediaService.Contracts.Nodes;

public sealed record NodeDto(
    Guid? ParentId,
    string Name,
    string Type
);
