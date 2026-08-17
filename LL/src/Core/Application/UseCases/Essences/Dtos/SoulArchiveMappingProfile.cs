using Application.Interfaces.Services.LL.Essences;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Application.UseCases.Essences.Dtos;

public sealed class SoulArchiveMappingProfile : Profile
{
    public SoulArchiveMappingProfile()
    {
        CreateMap<SoulArchive, SoulArchiveDto>().ConvertUsing<SoulArchiveConverter>();
        CreateMap<PlayerEssenceArchiveEntry, PlayerEssenceDto>().ConvertUsing<PlayerEssenceArchiveEntryConverter>();
    }
}

public sealed class SoulArchiveConverter : ITypeConverter<SoulArchive, SoulArchiveDto>
{
    public SoulArchiveDto Convert(SoulArchive source, SoulArchiveDto destination, ResolutionContext context) =>
        new(source.Essences.Select(x => context.Mapper.Map<PlayerEssenceDto>(x)).ToList(), source.EssenceDust);
}

public sealed class PlayerEssenceArchiveEntryConverter : ITypeConverter<PlayerEssenceArchiveEntry, PlayerEssenceDto>
{
    private readonly IEssenceDefinitionRepository _definitions;
    private readonly IEssenceProgressionService _progression;

    public PlayerEssenceArchiveEntryConverter(
        IEssenceDefinitionRepository definitions,
        IEssenceProgressionService progression)
    {
        _definitions = definitions;
        _progression = progression;
    }

    public PlayerEssenceDto Convert(PlayerEssenceArchiveEntry source, PlayerEssenceDto destination, ResolutionContext context)
    {
        var essence = source.Essence;
        var definition = _definitions.GetById(essence.EssenceDefinitionId) ?? new EssenceDefinition { Id = essence.EssenceDefinitionId, Name = essence.EssenceDefinitionId };
        var missing = GetMissingRequirements(essence, definition).ToList();
        var canAscend = CanAscend(essence);
        var canEvolve = !essence.IsEvolved && essence.AscensionTier >= definition.Evolution.RequiredAscensionTier;
        var levelCap = _progression.GetLevelCap(essence.AscensionTier);

        return new(
            essence.Id,
            essence.EssenceDefinitionId,
            definition.DisplayName,
            essence.Level,
            essence.CurrentXp,
            _progression.GetXpRequiredForNextLevel(essence, definition),
            levelCap,
            essence.AscensionTier,
            essence.IsEvolved,
            essence.IsFavorite,
            source.AttunedSlot,
            canAscend,
            canEvolve,
            missing,
            GetAscendInfo(essence, definition, canAscend),
            GetEvolveInfo(essence, definition, canEvolve),
            [],
            MapAbility(definition.ActiveAbility, essence),
            MapAbility(definition.PassiveAbility, essence),
            definition.Tags);
    }

    private IEnumerable<string> GetMissingRequirements(PlayerEssence essence, EssenceDefinition definition)
    {
        if (essence.AscensionTier >= EssenceProgressionConstants.MaxAscensionTier)
            yield return "Maximum Ascension Tier reached.";
        else
        {
            var requirement = EssenceProgressionConstants.GetAscensionRequirement(essence.AscensionTier + 1);
            if (essence.Level < requirement.RequiredLevel)
                yield return $"Reach Level {requirement.RequiredLevel} to ascend.";
        }
        if (essence.IsEvolved) yield return "Already evolved.";
        if (essence.AscensionTier < definition.Evolution.RequiredAscensionTier) yield return $"Reach Ascension Tier {definition.Evolution.RequiredAscensionTier}.";
    }

    private static bool CanAscend(PlayerEssence essence)
    {
        if (essence.AscensionTier >= EssenceProgressionConstants.MaxAscensionTier) return false;
        var requirement = EssenceProgressionConstants.GetAscensionRequirement(essence.AscensionTier + 1);
        return essence.Level >= requirement.RequiredLevel;
    }

    private EssenceAscendInfoDto GetAscendInfo(PlayerEssence essence, EssenceDefinition definition, bool canAscend)
    {
        var nextTier = essence.AscensionTier >= EssenceProgressionConstants.MaxAscensionTier ? (int?)null : essence.AscensionTier + 1;
        var cost = nextTier is null
            ? null
            : EssenceProgressionConstants.GetAscensionCost(nextTier.Value);
        var requirement = nextTier is null
            ? null
            : EssenceProgressionConstants.GetAscensionRequirement(nextTier.Value);
        var requiredItemId = cost?.ItemId;
        var requirements = new List<string>();

        if (essence.AscensionTier >= EssenceProgressionConstants.MaxAscensionTier)
        {
            requirements.Add("Already at maximum Ascension Tier.");
        }
        else
        {
            requirements.Add($"Reach Level {requirement!.RequiredLevel}.");
            requirements.Add($"Consume {cost!.Amount} {FormatItemName(requiredItemId)}.");
        }

        var effects = nextTier is null
            ? new List<string> { "No further Ascension bonuses are available." }
            : new List<string>
            {
                $"Raises the level cap to {_progression.GetLevelCap(nextTier.Value)}.",
                $"Improves ability values such as damage, healing, barriers, status strength, and summons based on the ability's effect type.",
                $"Reduces active ability cooldowns by up to {EssenceProgressionConstants.MaxActiveCooldownReduction:P0} at higher Ascension Tiers."
            };

        return new(
            canAscend,
            essence.AscensionTier,
            nextTier,
            requiredItemId,
            FormatItemName(requiredItemId),
            requirements,
            effects,
            requirement?.RequiredLevel,
            cost?.Amount,
            GetAscensionGrants(essence, definition, nextTier));
    }

    private static EssenceEvolveInfoDto GetEvolveInfo(PlayerEssence essence, EssenceDefinition definition, bool canEvolve)
    {
        var requirements = new List<string>();

        if (essence.IsEvolved)
            requirements.Add("Already evolved.");
        else
            requirements.Add($"Reach Ascension Tier {definition.Evolution.RequiredAscensionTier}.");

        if (!essence.IsEvolved)
            requirements.Add($"Consume 1 {FormatItemName(definition.Evolution.RequiredCatalystItemId)}.");

        var effects = new List<string>();
        if (!string.IsNullOrWhiteSpace(definition.Evolution.Description))
            effects.Add(definition.Evolution.Description);

        if (definition.Evolution.ActiveAbilityModifiers.Count > 0)
            effects.Add("Enhances the active ability.");

        if (definition.Evolution.PassiveAbilityModifiers.Count > 0)
            effects.Add("Enhances the passive ability.");

        if (definition.Evolution.AddsTags.Count > 0)
            effects.Add("Adds evolved tags: " + string.Join(", ", definition.Evolution.AddsTags) + ".");

        if (effects.Count == 0)
            effects.Add("Unlocks the Essence's evolved form.");

        return new(
            canEvolve,
            definition.Evolution.Name,
            definition.Evolution.Description,
            definition.Evolution.RequiredAscensionTier,
            definition.Evolution.RequiredCatalystItemId,
            FormatItemName(definition.Evolution.RequiredCatalystItemId),
            requirements,
            effects);
    }

    private static string FormatItemName(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return "required item";
        if (itemId.Equals(EssenceProgressionConstants.LesserMonsterCoreItemId, StringComparison.OrdinalIgnoreCase))
            return "Lesser Monster Core";
        if (itemId.Equals(EssenceProgressionConstants.GreaterMonsterCoreItemId, StringComparison.OrdinalIgnoreCase))
            return "Greater Monster Core";
        if (itemId.Equals(EssenceProgressionConstants.PrimalMonsterCoreItemId, StringComparison.OrdinalIgnoreCase))
            return "Primal Monster Core";

        var parts = itemId
            .Replace("item.", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(x => x.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(x => x.Length == 0 ? x : char.ToUpperInvariant(x[0]) + x[1..]);

        return string.Join(' ', parts);
    }

    private static EssenceAbilityDto MapAbility(AbilitySpec ability, PlayerEssence essence)
    {
        var scaledAbility = EssenceAbilityProgressionScaler.Apply(ability, essence.AscensionTier);
        return new(
            scaledAbility.Id,
            scaledAbility.Kind.ToString(),
            scaledAbility.Name,
            scaledAbility.Description,
            scaledAbility.CooldownTicks / 10d,
            AbilityTargetMapping.GetDistinctTargets(scaledAbility),
            scaledAbility.Tags,
            scaledAbility.Effects.Select(x => new EssenceEffectDto(
                x.Id,
                x.Operation.ToString(),
                x.Target.ToString(),
                x.BaseValue,
                x.BaseValue,
                x.Attribute?.ToString(),
                x.StatusId,
                x.DurationTicks > 0 ? x.DurationTicks / 10d : null,
                x.ScalingAttribute is { } attribute
                    ? [new EssenceEffectScalingDto(
                        attribute.ToString(),
                        x.ScalingCoefficient,
                        x.MaximumScalingCoefficient > x.ScalingCoefficient
                            ? x.MaximumScalingCoefficient
                            : null)]
                    : [],
                [])).ToList());
    }

    private IReadOnlyList<EssenceAscensionGrantDto> GetAscensionGrants(
        PlayerEssence essence,
        EssenceDefinition definition,
        int? nextTier)
    {
        if (nextTier is null)
            return [];

        var grants = new List<EssenceAscensionGrantDto>
        {
            new(
                "Level cap",
                _progression.GetLevelCap(essence.AscensionTier).ToString(CultureInfo.InvariantCulture),
                _progression.GetLevelCap(nextTier.Value).ToString(CultureInfo.InvariantCulture))
        };

        AddAbilityGrants(grants, definition.ActiveAbility, essence.AscensionTier, nextTier.Value);
        AddAbilityGrants(grants, definition.PassiveAbility, essence.AscensionTier, nextTier.Value);
        return grants;
    }

    private static void AddAbilityGrants(
        List<EssenceAscensionGrantDto> grants,
        AbilitySpec ability,
        int currentTier,
        int nextTier)
    {
        var current = EssenceAbilityProgressionScaler.Apply(ability, currentTier);
        var next = EssenceAbilityProgressionScaler.Apply(ability, nextTier);

        AddGrant(
            grants,
            $"{ability.Name} cooldown",
            FormatSeconds(current.CooldownTicks),
            FormatSeconds(next.CooldownTicks));

        for (var index = 0; index < Math.Min(current.Triggers.Count, next.Triggers.Count); index++)
        {
            AddGrant(
                grants,
                $"{ability.Name} trigger cooldown",
                FormatSeconds(current.Triggers[index].InternalCooldownTicks),
                FormatSeconds(next.Triggers[index].InternalCooldownTicks));
        }

        for (var index = 0; index < Math.Min(current.Effects.Count, next.Effects.Count); index++)
        {
            var currentEffect = current.Effects[index];
            var nextEffect = next.Effects[index];
            var effectLabel = current.Effects.Count == 1
                ? ability.Name
                : $"{ability.Name} · {FormatDisplayName(currentEffect.Operation.ToString())}";

            AddGrant(
                grants,
                effectLabel,
                FormatMagnitude(currentEffect),
                FormatMagnitude(nextEffect));
            AddGrant(
                grants,
                $"{effectLabel} duration",
                FormatSeconds(currentEffect.DurationTicks),
                FormatSeconds(nextEffect.DurationTicks));
            AddGrant(
                grants,
                $"{effectLabel} event scaling",
                FormatCoefficient(currentEffect.EventMagnitudeCoefficient),
                FormatCoefficient(nextEffect.EventMagnitudeCoefficient));
            AddGrant(
                grants,
                $"{effectLabel} condition scaling",
                FormatCoefficient(currentEffect.ConditionScalingCoefficient),
                FormatCoefficient(nextEffect.ConditionScalingCoefficient));
            AddGrant(
                grants,
                $"{effectLabel} status scaling",
                FormatCoefficient(currentEffect.StatusScalingCoefficient),
                FormatCoefficient(nextEffect.StatusScalingCoefficient));

            if (currentEffect.Operation == AbilityEffectOperation.Summon)
            {
                AddGrant(
                    grants,
                    $"{effectLabel} power",
                    FormatMultiplier(currentEffect.SummonPowerMultiplier),
                    FormatMultiplier(nextEffect.SummonPowerMultiplier));
                AddGrant(
                    grants,
                    $"{effectLabel} health",
                    FormatMultiplier(currentEffect.SummonHealthMultiplier),
                    FormatMultiplier(nextEffect.SummonHealthMultiplier));
            }
        }
    }

    private static void AddGrant(
        List<EssenceAscensionGrantDto> grants,
        string label,
        string? currentValue,
        string? nextValue)
    {
        if (string.IsNullOrWhiteSpace(currentValue)
            || string.IsNullOrWhiteSpace(nextValue)
            || currentValue.Equals(nextValue, StringComparison.Ordinal))
            return;

        grants.Add(new(label, currentValue, nextValue));
    }

    private static string? FormatMagnitude(AbilityEffectSpec effect)
    {
        var hasScaling = effect.ScalingAttribute is not null && effect.ScalingCoefficient != 0;
        if (!hasScaling && effect.BaseValue == 0)
            return null;

        var parts = new List<string>();
        if (effect.BaseValue != 0)
        {
            var unit = UsesPercentageBaseValue(effect.Operation) ? "%" : string.Empty;
            parts.Add($"{effect.BaseValue.ToString(CultureInfo.InvariantCulture)}{unit}");
        }

        if (hasScaling)
        {
            var isPowerScaling = effect.ScalingAttribute == AttributeType.Power;
            var minimum = isPowerScaling
                ? FormatWholeNumber(effect.ScalingCoefficient * 100d)
                : FormatNumber(effect.ScalingCoefficient * 100d);
            var maximum = effect.MaximumScalingCoefficient > effect.ScalingCoefficient
                ? $"–{(isPowerScaling
                    ? FormatWholeNumber(effect.MaximumScalingCoefficient * 100d)
                    : FormatNumber(effect.MaximumScalingCoefficient * 100d))}"
                : string.Empty;
            var attribute = isPowerScaling
                ? string.Empty
                : $" {FormatDisplayName(effect.ScalingAttribute!.Value.ToString())}";
            parts.Add($"{minimum}{maximum}%{attribute}");
        }

        return string.Join(" + ", parts);
    }

    private static string? FormatSeconds(int ticks) =>
        ticks > 0 ? $"{FormatNumber(ticks / 10d)}s" : null;

    private static string? FormatCoefficient(float coefficient) =>
        coefficient != 0 ? $"{FormatNumber(coefficient * 100d)}%" : null;

    private static string FormatMultiplier(double multiplier) =>
        $"{FormatNumber(multiplier * 100d)}%";

    private static string FormatNumber(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatWholeNumber(double value) =>
        Math.Round(value, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);

    private static string FormatDisplayName(string value) =>
        Regex.Replace(value, "(?<=[a-z0-9])(?=[A-Z])", " ");

    private static bool UsesPercentageBaseValue(AbilityEffectOperation operation) =>
        operation is AbilityEffectOperation.ModifyRegenerationRate
            or AbilityEffectOperation.ModifyHealingReceived
            or AbilityEffectOperation.ModifyDamageDealt
            or AbilityEffectOperation.ModifyDamageTaken
            or AbilityEffectOperation.ModifyDamageTakenFromCondition
            or AbilityEffectOperation.ModifyNextBasicAttackDamage
            or AbilityEffectOperation.ModifyNextBasicAttackArmorPenetration;
}
