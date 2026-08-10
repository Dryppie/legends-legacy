using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.UseCases.Achievements.Dtos;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Components.Attributes;
using Domain.Helpers.Constants;
using Domain.Models.Achievements;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Guilds;

namespace Application.UseCases.Characters.Dtos;
public class CharacterOverviewDto : IMapFrom<Character>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public long Experience { get; set; }
    public long ExperienceUntilNextLevel { get; set; }
    public int CraftingLevel { get; set; }
    public int CraftingExperience { get; set; }
    public int CraftingExperienceUntilNextLevel { get; set; }
    public OverallPowerRating? Power { get; set; }
    public List<EntityAttribute> BaseAttributes { get; set; } = [];
    public List<EntityAttribute> BaseCombatAttributes { get; set; } = [];
    public EssenceLoadoutDto? ActiveEssenceLoadout { get; set; }
    public EquippedTitleDto? EquippedTitle { get; set; }
    public CharacterGuildDto? Guild { get; set; }
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Character, CharacterOverviewDto>()
            .ConvertUsing<CharacterOverviewConverter>();
    }
}

public sealed record CharacterGuildDto(Guid Id, string Name, string Tag)
{
    public static CharacterGuildDto? From(GuildMember? membership) => membership is null
        ? null
        : new CharacterGuildDto(
            membership.GuildId,
            membership.Guild.Name,
            membership.Guild.Tag);
}

public sealed class CharacterOverviewConverter : ITypeConverter<Character, CharacterOverviewDto>
{
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly TimeProvider _timeProvider;

    public CharacterOverviewConverter(
        IEssenceDefinitionRepository essenceDefinitions,
        TimeProvider? timeProvider = null)
    {
        _essenceDefinitions = essenceDefinitions;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public CharacterOverviewDto Convert(Character source, CharacterOverviewDto destination, ResolutionContext context)
    {
        var craftingProfession = source.Professions
            .Where(profession => (int)profession.ProfessionType is 1 or 2 or 3)
            .OrderByDescending(profession => profession.Level)
            .ThenByDescending(profession => profession.Experience)
            .FirstOrDefault();
        var craftingLevel = craftingProfession?.Level ?? 1;

        return new CharacterOverviewDto
        {
            Id = source.Id,
            Name = source.Name,
            Level = source.Level,
            Experience = source.Experience,
            ExperienceUntilNextLevel = source.ExperienceUntilNextLevel,
            CraftingLevel = craftingLevel,
            CraftingExperience = (int)MathF.Floor(craftingProfession?.Experience ?? 0),
            CraftingExperienceUntilNextLevel = EntityLevelConstants.XP_REQUIRED(craftingLevel),
            BaseAttributes = source.BaseAttributes.ToList(),
            BaseCombatAttributes = source.BaseCombatAttributes.Select(kvp => new EntityAttribute
            {
                EntityId = source.Id,
                AttributeType = kvp.Key,
                Value = kvp.Value
            }).ToList(),
            ActiveEssenceLoadout = MapActiveLoadout(source),
            EquippedTitle = MapEquippedTitle(source),
            LastSeenAt = source.CharacterAction?.UpdatedAt,
            IsOnline = source.CharacterAction?.UpdatedAt >
                       _timeProvider.GetUtcNow().Subtract(PlayerActivityConstants.OnlineWindow)
        };
    }

    private static EquippedTitleDto? MapEquippedTitle(Character source)
    {
        var title = source.EquippedTitleDefinition;
        if (title is null) return null;

        return new EquippedTitleDto
        {
            Key = title.Key,
            Name = title.Name,
            DisplayPosition = source.EquippedTitleDisplayPosition,
            DisplayName = TitleDisplayFormatter.Format(
                source.Name,
                title.Name,
                source.EquippedTitleDisplayPosition)
        };
    }

    private EssenceLoadoutDto? MapActiveLoadout(Character source)
    {
        var loadout = source.EssenceLoadouts.FirstOrDefault(x => x.IsActive);
        if (loadout is null) return null;

        return new EssenceLoadoutDto(
            loadout.Id,
            loadout.Name,
            loadout.IsActive,
            loadout.Slots
                .OrderBy(slot => slot.SlotIndex)
                .Select(MapSlot)
                .ToList());
    }

    private EssenceLoadoutSlotDto MapSlot(EssenceLoadoutSlot slot)
    {
        var definition = slot.PlayerEssence is null
            ? null
            : _essenceDefinitions.GetById(slot.PlayerEssence.EssenceDefinitionId);

        return new(
            slot.SlotIndex,
            slot.PlayerEssenceId,
            slot.PlayerEssence?.EssenceDefinitionId,
            definition?.Name);
    }
}
