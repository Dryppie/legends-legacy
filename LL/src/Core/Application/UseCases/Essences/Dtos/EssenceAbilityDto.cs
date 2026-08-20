namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceAbilityDto(
    string Id,
    string Kind,
    string Name,
    string Description,
    double CooldownSeconds,
    int ThreatValue,
    float ThreatMultiplier,
    double EstimatedThreatPerSecond,
    bool HasMaintainedThreat,
    IReadOnlyList<string> Targets,
    IReadOnlyList<string> Tags,
    IReadOnlyList<EssenceEffectDto> Effects);
