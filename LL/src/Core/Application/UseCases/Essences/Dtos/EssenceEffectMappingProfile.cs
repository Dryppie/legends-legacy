using AutoMapper;
using Domain.Models.Combat.Abilities;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceEffectMappingProfile : Profile
{
    public EssenceEffectMappingProfile()
    {
        CreateMap<AbilityEffectSpec, EssenceEffectDto>().ConvertUsing<AbilityEffectSpecConverter>();
    }
}

public sealed class AbilityEffectSpecConverter : ITypeConverter<AbilityEffectSpec, EssenceEffectDto>
{
    public EssenceEffectDto Convert(AbilityEffectSpec source, EssenceEffectDto destination, ResolutionContext context) =>
        new(
            source.Id,
            source.Operation.ToString(),
            source.Target.ToString(),
            source.BaseValue,
            source.BaseValue,
            source.Attribute?.ToString(),
            source.StatusId,
            source.DurationTicks > 0 ? source.DurationTicks / 10d : null,
            source.ScalingAttribute is { } attribute
                ? [new EssenceEffectScalingDto(attribute.ToString(), source.ScalingCoefficient)]
                : [],
            []);
}
