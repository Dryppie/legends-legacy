namespace Domain.Models.Attributes;

public sealed record AttributeDefinition(
    AttributeType AttributeType,
    string Description,
    bool IsContentFacing = true);
