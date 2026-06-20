using Application.Interfaces.Services.LL.Essences;
using AutoMapper;
using Domain.Models.Combat.Abilities.V2;
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
        var canAscend = essence.Level >= _progression.GetLevelCap(essence.AscensionTier) && essence.AscensionTier < 3;
        var canEvolve = !essence.IsEvolved && essence.AscensionTier >= definition.Evolution.RequiredAscensionTier;

        return new(
            essence.Id,
            essence.EssenceDefinitionId,
            definition.Name,
            essence.Level,
            essence.CurrentXp,
            _progression.GetXpRequiredForNextLevel(essence, definition),
            essence.AscensionTier,
            _progression.GetLevelCap(essence.AscensionTier),
            essence.IsEvolved,
            essence.IsFavorite,
            source.AttunedSlot,
            canAscend,
            canEvolve,
            missing,
            GetAscendInfo(essence, definition, canAscend),
            GetEvolveInfo(essence, definition, canEvolve),
            GetAttributeBonuses(definition, essence).ToList(),
            MapAbility(definition.ActiveAbility, essence),
            MapAbility(definition.PassiveAbility, essence));
    }

    private IEnumerable<string> GetMissingRequirements(PlayerEssence essence, EssenceDefinition definition)
    {
        if (essence.Level < _progression.GetLevelCap(essence.AscensionTier)) yield return "Reach current Ascension Tier level cap.";
        if (essence.AscensionTier >= 3) yield return "Maximum Ascension Tier reached.";
        if (essence.IsEvolved) yield return "Already evolved.";
        if (essence.AscensionTier < definition.Evolution.RequiredAscensionTier) yield return $"Reach Ascension Tier {definition.Evolution.RequiredAscensionTier}.";
    }

    private EssenceAscendInfoDto GetAscendInfo(PlayerEssence essence, EssenceDefinition definition, bool canAscend)
    {
        var nextTier = essence.AscensionTier >= 3 ? (int?)null : essence.AscensionTier + 1;
        var currentCap = _progression.GetLevelCap(essence.AscensionTier);
        var cost = nextTier is null
            ? null
            : EssenceProgressionConstants.GetAscensionCost(nextTier.Value);
        var requiredItemId = cost?.ItemId;
        var requirements = new List<string>();

        if (essence.AscensionTier >= 3)
        {
            requirements.Add("Already at maximum Ascension Tier.");
        }
        else
        {
            requirements.Add($"Reach Level {currentCap} in Ascension Tier {essence.AscensionTier}.");
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
            ability.Effects.FirstOrDefault()?.Target.ToString() ?? AbilityTargetSelectorV2.CurrentTarget.ToString(),
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
