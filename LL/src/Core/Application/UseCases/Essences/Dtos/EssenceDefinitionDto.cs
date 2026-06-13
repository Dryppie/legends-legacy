using Domain.Models.Items;

namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceDefinitionDto(
    string Id,
    string SourceMonsterId,
    string Name,
    string Description,
    Rarity Rarity,
    IReadOnlyDictionary<string, IReadOnlyList<string>> TagsByCategory,
    IReadOnlyList<EssenceAttributeBonusDto> AttributeBonuses,
    EssenceAbilityDto ActiveAbility,
    EssenceAbilityDto PassiveAbility,
    EssenceEvolutionDto Evolution,
    EssenceDropDto Drop);
