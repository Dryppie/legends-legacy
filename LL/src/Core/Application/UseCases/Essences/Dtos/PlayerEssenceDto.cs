namespace Application.UseCases.Essences.Dtos;

public sealed record PlayerEssenceDto(
    Guid Id,
    string EssenceDefinitionId,
    string Name,
    int Level,
    int CurrentXp,
    int XpRequiredForNextLevel,
    int AscensionTier,
    int TierLevelCap,
    bool IsEvolved,
    bool IsFavorite,
    int? AttunedSlot,
    bool CanAscend,
    bool CanEvolve,
    IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<EssenceAttributeBonusDto> CurrentAttributeBonuses,
    EssenceAbilityDto ActiveAbility,
    EssenceAbilityDto PassiveAbility);
