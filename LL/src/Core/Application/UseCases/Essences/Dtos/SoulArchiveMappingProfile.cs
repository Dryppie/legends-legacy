using Application.Interfaces.Services.LL.Essences;
using AutoMapper;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;

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
        var canUpgradePotential = essence.PotentialTier < EssenceProgressionConstants.MaxPotentialTier
            && essence.Level >= _progression.GetLevelCapForPotential(essence.PotentialTier);
        var canAscend = CanAscend(essence);
        var canEvolve = !essence.IsEvolved && essence.AscensionTier >= definition.Evolution.RequiredAscensionTier;
        var potentialCap = _progression.GetLevelCapForPotential(essence.PotentialTier);

        return new(
            essence.Id,
            essence.EssenceDefinitionId,
            definition.Name,
            essence.Level,
            essence.CurrentXp,
            _progression.GetXpRequiredForNextLevel(essence, definition),
            essence.NativeRegion,
            essence.PotentialTier,
            potentialCap,
            essence.AscensionTier,
            potentialCap,
            essence.IsEvolved,
            essence.IsFavorite,
            source.AttunedSlot,
            canAscend,
            canUpgradePotential,
            canEvolve,
            missing,
            GetPotentialInfo(essence, canUpgradePotential),
            GetAscendInfo(essence, definition, canAscend),
            GetEvolveInfo(essence, definition, canEvolve),
            GetAttributeBonuses(definition, essence).ToList(),
            MapAbility(definition.ActiveAbility, essence),
            MapAbility(definition.PassiveAbility, essence));
    }

    private IEnumerable<string> GetMissingRequirements(PlayerEssence essence, EssenceDefinition definition)
    {
        if (essence.Level < _progression.GetLevelCapForPotential(essence.PotentialTier)) yield return "Reach current Potential level cap.";
        if (essence.PotentialTier >= EssenceProgressionConstants.MaxPotentialTier) yield return "Maximum Potential reached.";
        if (essence.AscensionTier >= EssenceProgressionConstants.MaxAscensionTier) yield return "Maximum Ascension Tier reached.";
        if (essence.IsEvolved) yield return "Already evolved.";
        if (essence.AscensionTier < definition.Evolution.RequiredAscensionTier) yield return $"Reach Ascension Tier {definition.Evolution.RequiredAscensionTier}.";
    }

    private static bool CanAscend(PlayerEssence essence)
    {
        if (essence.AscensionTier >= EssenceProgressionConstants.MaxAscensionTier) return false;
        var requirement = EssenceProgressionConstants.GetAscensionRequirement(essence.AscensionTier + 1);
        return essence.Level >= requirement.RequiredLevel
               && essence.PotentialTier >= requirement.RequiredPotentialTier;
    }

    private EssencePotentialInfoDto GetPotentialInfo(PlayerEssence essence, bool canUpgrade)
    {
        var nextTier = essence.PotentialTier >= EssenceProgressionConstants.MaxPotentialTier
            ? (int?)null
            : essence.PotentialTier + 1;
        var currentCap = _progression.GetLevelCapForPotential(essence.PotentialTier);
        var nextCap = nextTier is null
            ? (int?)null
            : _progression.GetLevelCapForPotential(nextTier.Value);
        var cost = nextTier is null
            ? null
            : EssenceProgressionConstants.GetPotentialUpgradeCost(essence.PotentialTier);
        var requirements = new List<string>();

        if (nextTier is null)
        {
            requirements.Add("Already at maximum Potential.");
        }
        else
        {
            requirements.Add($"Reach Level {currentCap}.");
            requirements.Add($"Consume {cost!.Amount} {FormatItemName(cost.ItemId)}.");
        }

        var effects = nextTier is null
            ? new List<string> { "No further Potential upgrades are available." }
            : new List<string> { $"Raises the level cap to {nextCap}.", "Allows this Essence to gain more stat levels." };

        return new(
            canUpgrade,
            essence.PotentialTier,
            nextTier,
            currentCap,
            nextCap,
            cost?.ItemId,
            FormatItemName(cost?.ItemId),
            requirements,
            effects);
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
            requirements.Add($"Reach Potential Tier {requirement.RequiredPotentialTier}.");
            requirements.Add($"Consume {cost!.Amount} {FormatItemName(requiredItemId)}.");
        }

        var effects = nextTier is null
            ? new List<string> { "No further Ascension bonuses are available." }
            : new List<string>
            {
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
            effects);
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

        if (definition.Evolution.AttributeModifierChanges.Count > 0)
            effects.Add($"Adds {definition.Evolution.AttributeModifierChanges.Count} evolved attribute bonus changes.");

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

        const string potentialCorePrefix = "item.essence_potential_core.region_";
        if (itemId.StartsWith(potentialCorePrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(itemId[potentialCorePrefix.Length..], out var region))
        {
            return $"Region {region} Potential Core";
        }

        var parts = itemId
            .Replace("item.", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(x => x.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(x => x.Length == 0 ? x : char.ToUpperInvariant(x[0]) + x[1..]);

        return string.Join(' ', parts);
    }

    private static IEnumerable<EssenceAttributeBonusDto> GetAttributeBonuses(EssenceDefinition definition, PlayerEssence essence)
    {
        var bonuses = definition.AttributeBonuses.Concat(essence.IsEvolved ? definition.Evolution.AttributeModifierChanges : []);
        foreach (var bonus in bonuses)
        {
            var value = EssenceProgressionConstants.ScaleAttributeBonus(bonus.BaseValue, essence.Level);
            yield return new(bonus.Attribute, bonus.ModifierKind.ToString(), bonus.BaseValue, value);
        }
    }

    private EssenceAbilityDto MapAbility(AbilitySpec ability, PlayerEssence essence) =>
        new(
            ability.Id,
            ability.Kind.ToString(),
            ability.Name,
            ability.Description,
            ability.CooldownTicks / 10d,
            ability.Effects.FirstOrDefault()?.Target.ToString() ?? AbilityTargetSelector.CurrentTarget.ToString(),
            ability.Tags,
            ability.Effects.Select(x => new EssenceEffectDto(
                x.Id,
                x.Operation.ToString(),
                x.Target.ToString(),
                x.BaseValue,
                EssenceProgressionConstants.ScaleAbilityValue(x.BaseValue, essence.Level, essence.AscensionTier, x.Operation.ToString()),
                x.Attribute?.ToString(),
                x.StatusId,
                x.DurationTicks > 0
                    ? EssenceProgressionConstants.ScaleEffectDurationSeconds(x.DurationTicks / 10d, essence.AscensionTier, x.Operation.ToString(), x.StatusId)
                    : null,
                x.ScalingAttribute is { } attribute
                    ? [new EssenceEffectScalingDto(attribute.ToString(), x.ScalingCoefficient)]
                    : [],
                [])).ToList());
}
