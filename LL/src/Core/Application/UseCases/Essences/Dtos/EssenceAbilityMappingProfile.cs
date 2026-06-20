using AutoMapper;
using Domain.Models.Combat.Abilities.V2;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceAbilityMappingProfile : Profile
{
    public EssenceAbilityMappingProfile()
    {
        CreateMap<AbilitySpec, EssenceAbilityDto>().ConvertUsing<AbilitySpecConverter>();
    }
}

public sealed class AbilitySpecConverter : ITypeConverter<AbilitySpec, EssenceAbilityDto>
{
    public EssenceAbilityDto Convert(AbilitySpec source, EssenceAbilityDto destination, ResolutionContext context) =>
        new(
            source.Id,
            source.Kind.ToString(),
            source.Name,
            source.Description,
            source.CooldownTicks / 10d,
            source.Effects.FirstOrDefault()?.Target.ToString() ?? AbilityTargetSelectorV2.CurrentTarget.ToString(),
            source.Tags,
            source.Effects.Select(x => context.Mapper.Map<EssenceEffectDto>(x)).ToList());
}
