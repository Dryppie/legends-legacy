namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceEffectDto(
    string Id,
    string Type,
    string Target,
    double BaseValue,
    double CurrentValue,
    string? Attribute,
    string? Status,
    double? DurationSeconds,
    IReadOnlyList<EssenceEffectScalingDto> Scaling,
    IReadOnlyList<EssenceEffectDto> NestedEffects);

public sealed record EssenceEffectScalingDto(string Attribute, double Coefficient);
