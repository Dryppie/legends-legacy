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
            source.EventMagnitudeCoefficient,
            source.ConditionScalingCoefficient,
            source.StatusScalingCoefficient,
            source.SummonPowerMultiplier,
            source.SummonHealthMultiplier,
            GetDescriptionScalingAttribute(source) is { } attribute
                ? [new EssenceEffectScalingDto(
                    attribute.ToString(),
                    source.ScalingCoefficient,
                    source.MaximumScalingCoefficient > source.ScalingCoefficient
                        ? source.MaximumScalingCoefficient
                        : null)]
                : [],
            []);

    private static Domain.Models.Attributes.AttributeType? GetDescriptionScalingAttribute(AbilityEffectSpec source) =>
        source.ScalingAttribute
        ?? (source.Operation == AbilityEffectOperation.ModifyAttributePercentOfInitial
            ? source.Attribute
            : null);
}
