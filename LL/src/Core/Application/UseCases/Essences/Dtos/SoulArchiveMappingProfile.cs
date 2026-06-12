using Application.Interfaces.Services.LL.Essences;
using AutoMapper;
using Domain.Models.AbilityDefinitions;
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
            essence.Level >= _progression.GetLevelCap(essence.AscensionTier) && essence.AscensionTier < 3,
            !essence.IsEvolved && essence.AscensionTier >= definition.Evolution.RequiredAscensionTier,
            missing,
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

    private static IEnumerable<EssenceAttributeBonusDto> GetAttributeBonuses(EssenceDefinition definition, PlayerEssence essence)
    {
        var bonuses = definition.AttributeBonuses.Concat(essence.IsEvolved ? definition.Evolution.AttributeModifierChanges : []);
        foreach (var bonus in bonuses)
        {
            var value = EssenceProgressionConstants.ScaleAttributeBonus(bonus.BaseValue, essence.Level);
            yield return new(bonus.Attribute, bonus.ModifierKind.ToString(), bonus.BaseValue, value);
        }
    }

    private static EssenceAbilityDto MapAbility(AbilityDefinition ability, PlayerEssence essence) =>
        new(
            ability.Id,
            ability.Kind,
            ability.Name,
            ability.Description,
            ability.CooldownSeconds,
            ability.Targeting,
            ability.Tags,
            ability.Effects.Select(x => new EssenceEffectDto(
                x.Id,
                x.Type,
                x.Target,
                EssenceProgressionConstants.ScaleAbilityValue(x.Scaling.BaseValue, essence.Level),
                x.Attribute,
                x.Status,
                x.DurationSeconds)).ToList());
}
