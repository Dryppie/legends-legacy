using AutoMapper;
using Domain.Models.Combat.Abilities;
using System.Globalization;
using System.Text.RegularExpressions;

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
    private static readonly Regex TriggerCooldownPlaceholderPattern = new(
        @"\{triggerCooldown(?<index>\d*)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public EssenceAbilityDto Convert(AbilitySpec source, EssenceAbilityDto destination, ResolutionContext context) =>
        new(
            source.Id,
            source.Kind.ToString(),
            source.Name,
            ResolveDescription(source),
            source.CooldownTicks / 10d,
            AbilityThreatRules.GetThreatValue(source),
            source.ThreatMultiplier,
            AbilityThreatRules.GetEstimatedThreatPerSecond(source),
            AbilityThreatRules.HasMaintainedThreat(source),
            AbilityTargetMapping.GetDistinctTargets(source),
            source.Tags,
            source.Effects.Select(x => context.Mapper.Map<EssenceEffectDto>(x)).ToList());

    private static string ResolveDescription(AbilitySpec ability)
    {
        var cooldowns = ability.Triggers
            .Where(trigger => trigger.InternalCooldownTicks > 0)
            .Select(trigger => trigger.InternalCooldownTicks)
            .ToArray();

        return TriggerCooldownPlaceholderPattern.Replace(
            ability.Description,
            match =>
            {
                var rawIndex = match.Groups["index"].Value;
                var index = string.IsNullOrEmpty(rawIndex)
                    ? 0
                    : int.Parse(rawIndex, CultureInfo.InvariantCulture) - 1;
                if (index < 0 || index >= cooldowns.Length)
                    return match.Value;

                var ticks = cooldowns[index];
                var seconds = (ticks / 10d).ToString("0.#", CultureInfo.InvariantCulture);
                return $"{seconds} {(ticks == 10 ? "second" : "seconds")}";
            });
    }
}
