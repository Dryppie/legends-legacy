namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceAscendInfoDto(
    bool CanPerform,
    int CurrentTier,
    int? NextTier,
    string? RequiredItemId,
    string? RequiredItemName,
    IReadOnlyList<string> Requirements,
    IReadOnlyList<string> Effects);
