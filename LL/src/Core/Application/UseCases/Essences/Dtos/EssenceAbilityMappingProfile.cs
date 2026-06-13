using AutoMapper;
using Domain.Models.AbilityDefinitions;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceAbilityMappingProfile : Profile
{
    public EssenceAbilityMappingProfile()
    {
        CreateMap<AbilityDefinition, EssenceAbilityDto>().ConvertUsing<AbilityDefinitionConverter>();
    }
}

public sealed class AbilityDefinitionConverter : ITypeConverter<AbilityDefinition, EssenceAbilityDto>
{
    public EssenceAbilityDto Convert(AbilityDefinition source, EssenceAbilityDto destination, ResolutionContext context) =>
        new(
            source.Id,
            source.Kind,
            source.Name,
            source.Description,
            source.CooldownSeconds,
            source.Targeting,
            source.Tags,
            source.Effects.Select(x => context.Mapper.Map<EssenceEffectDto>(x)).ToList());
}
