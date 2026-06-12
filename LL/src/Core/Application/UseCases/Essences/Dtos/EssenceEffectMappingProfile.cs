using AutoMapper;
using Domain.Interfaces.Combat;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.ResourceCosts;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceEffectMappingProfile : Profile
{
    public EssenceEffectMappingProfile()
    {
        CreateMap<AbilityEffectDefinition, EssenceEffectDto>().ConvertUsing<AbilityEffectDefinitionConverter>();
    }
}

public sealed class AbilityEffectDefinitionConverter : ITypeConverter<AbilityEffectDefinition, EssenceEffectDto>
{
    private readonly IStatusDefinitionService _statuses;

    public AbilityEffectDefinitionConverter(IStatusDefinitionService statuses)
    {
        _statuses = statuses;
    }

    public EssenceEffectDto Convert(AbilityEffectDefinition source, EssenceEffectDto destination, ResolutionContext context) =>
        new(
            source.Id,
            source.Type,
            source.Target,
            source.Scaling.BaseValue,
            source.Scaling.BaseValue,
            source.Attribute,
            source.Status,
            source.DurationSeconds,
            source.Scaling.AttributeScaling
                .Select(x => new EssenceEffectScalingDto(x.Attribute.ToString(), x.Coefficient))
                .ToList(),
            GetNestedEffects(source).ToList());

    private IEnumerable<EssenceEffectDto> GetNestedEffects(AbilityEffectDefinition source)
    {
        if (!source.Type.Equals(AbilityEffectType.ApplyStatus, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(source.Status)
            || !_statuses.TryGetById(source.Status, out var status))
        {
            yield break;
        }

        foreach (var effect in status.Triggers.SelectMany(x => x.Actions))
        {
            if (MapNestedEffect(effect) is { } mapped)
            {
                yield return mapped;
            }
        }
    }

    private static EssenceEffectDto? MapNestedEffect(EffectDefinition effect)
    {
        if (effect.Action is not CombatEffectAction action)
        {
            return null;
        }

        var type = action.Operation switch
        {
            CombatEffectOperation.Damage => AbilityEffectType.Damage,
            CombatEffectOperation.RestoreResource when action.Resource == ResourceType.Health => AbilityEffectType.Heal,
            CombatEffectOperation.RestoreResource when action.Resource == ResourceType.Barrier => AbilityEffectType.GrantBarrier,
            CombatEffectOperation.ModifyAttribute => AbilityEffectType.ModifyAttribute,
            CombatEffectOperation.ApplyStatus => AbilityEffectType.ApplyStatus,
            CombatEffectOperation.ModifyStatusEffect => AbilityEffectType.ModifyStatusEffect,
            _ => null
        };

        if (type is null)
        {
            return null;
        }

        var scaling = action.ScalingAttribute is { } attribute
            ? new[] { new EssenceEffectScalingDto(attribute.ToString(), action.ScalingMultiplier) }
            : Array.Empty<EssenceEffectScalingDto>();

        return new EssenceEffectDto(
            effect.Log,
            type,
            effect.Targeting.ToString(),
            action.Magnitude,
            action.Magnitude,
            action.Attribute?.ToString(),
            action.StatusId,
            null,
            scaling,
            []);
    }
}
