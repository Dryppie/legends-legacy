namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceEvolveInfoDto(
    bool CanPerform,
    string Name,
    string Description,
    int RequiredAscensionTier,
    string RequiredItemId,
    string RequiredItemName,
    IReadOnlyList<string> Requirements,
    IReadOnlyList<string> Effects);
