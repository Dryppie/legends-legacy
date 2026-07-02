namespace Application.UseCases.Essences.Dtos;

public sealed record EssencePotentialInfoDto(
    bool CanPerform,
    int CurrentTier,
    int? NextTier,
    int CurrentLevelCap,
    int? NextLevelCap,
    string? RequiredItemId,
    string? RequiredItemName,
    IReadOnlyList<string> Requirements,
    IReadOnlyList<string> Effects);
