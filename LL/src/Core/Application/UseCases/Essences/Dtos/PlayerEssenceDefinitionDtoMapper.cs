using AutoMapper;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;

namespace Application.UseCases.Essences.Dtos;

public static class PlayerEssenceDefinitionDtoMapper
{
    public static EssenceDefinitionDto Map(
        EssenceDefinition definition,
        PlayerEssence essence,
        IMapperBase mapper)
    {
        var dto = mapper.Map<EssenceDefinitionDto>(definition);
        if (essence.AscensionTier <= 0)
            return dto;

        return dto with
        {
            ActiveAbility = mapper.Map<EssenceAbilityDto>(
                EssenceAbilityProgressionScaler.Apply(definition.ActiveAbility, essence.AscensionTier)),
            PassiveAbility = mapper.Map<EssenceAbilityDto>(
                EssenceAbilityProgressionScaler.Apply(definition.PassiveAbility, essence.AscensionTier))
        };
    }
}
