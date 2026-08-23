using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.UseCases.Achievements.Dtos;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Helpers.Constants;
using Domain.Models.Achievements;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Guilds;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Professions.Gathering;

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
    public List<GatheringProfessionOverviewDto> GatheringProfessions { get; set; } = [];
    public OverallPowerRating? Power { get; set; }
    public List<EntityAttribute> BaseAttributes { get; set; } = [];
    public List<EntityAttribute> BaseCombatAttributes { get; set; } = [];
    public List<EntityAttribute> EquipmentRatings { get; set; } = [];
    public EssenceLoadoutDto? EssenceLoadout { get; set; }
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
            GatheringProfessions = MapGatheringProfessions(source),
            BaseAttributes = MapBaseAttributes(source),
            BaseCombatAttributes = MapBaseCombatAttributes(source),
            EquipmentRatings = AttributeCalculator
                .CollectRawEquipmentRatings(source.EquipmentSlots
                    .Where(slot => slot.EquipmentInstance is not null)
                    .Select(slot => slot.EquipmentInstance!))
                .OrderBy(entry => entry.Key)
                .Select(entry => new EntityAttribute
                {
                    EntityId = source.Id,
                    AttributeType = entry.Key,
                    Value = (float)entry.Value
                })
                .ToList(),
            EssenceLoadout = MapDefaultLoadout(source, context),
            EquippedTitle = MapEquippedTitle(source),
            LastSeenAt = source.CharacterAction?.UpdatedAt,
            IsOnline = source.CharacterAction?.UpdatedAt >
                       _timeProvider.GetUtcNow().Subtract(PlayerActivityConstants.OnlineWindow)
        };
    }

    private static List<GatheringProfessionOverviewDto> MapGatheringProfessions(Character source)
    {
        var professions = source.Professions
            .Where(profession => GatheringProfessionProgression.IsGatheringProfession(profession.ProfessionType))
            .ToDictionary(profession => profession.ProfessionType);

        return new[]
            {
                ProfessionType.Mining,
                ProfessionType.Woodcutting,
                ProfessionType.Skinning
            }
            .Select(professionType =>
            {
                professions.TryGetValue(professionType, out var profession);
                var level = profession?.Level ?? 1;

                return new GatheringProfessionOverviewDto
                {
                    ProfessionType = professionType,
                    Level = level,
                    Experience = (int)MathF.Floor(profession?.Experience ?? 0),
                    ExperienceUntilNextLevel = GatheringProfessionProgression.GetRequiredExperience(level)
                };
            })
            .ToList();
    }

    private static List<EntityAttribute> MapBaseAttributes(Character source)
    {
        var attributes = source.BaseAttributes.ToList();
        if (attributes.All(attribute => attribute.AttributeType != AttributeType.Threat))
        {
            attributes.Add(new EntityAttribute
            {
                EntityId = source.Id,
                AttributeType = AttributeType.Threat,
                Value = EntityBaseAttributeHelper.BaseThreat
            });
        }

        return attributes;
    }

    private static List<EntityAttribute> MapBaseCombatAttributes(Character source)
    {
        var attributes = source.BaseCombatAttributes.Select(kvp => new EntityAttribute
        {
            EntityId = source.Id,
            AttributeType = kvp.Key,
            Value = kvp.Value
        }).ToList();
        if (attributes.All(attribute => attribute.AttributeType != AttributeType.Threat))
        {
            attributes.Add(new EntityAttribute
            {
                EntityId = source.Id,
                AttributeType = AttributeType.Threat,
                Value = source.BaseAttributes
                    .FirstOrDefault(attribute => attribute.AttributeType == AttributeType.Threat)
                    ?.Value ?? EntityBaseAttributeHelper.BaseThreat
            });
        }

        return attributes;
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

    private EssenceLoadoutDto? MapDefaultLoadout(Character source, ResolutionContext context)
    {
        var loadout = EssenceLoadoutSelection.Select(source.EssenceLoadouts, EssenceCombatActivity.None);
        if (loadout is null) return null;

        return new EssenceLoadoutDto(
            loadout.Id,
            loadout.Name,
            Enum.GetValues<EssenceCombatActivity>()
                .Where(activity => EssenceLoadoutSelection.IsValidSingleActivity(activity) && loadout.AutoUseActivities.HasFlag(activity))
                .ToList(),
            loadout.Slots
                .OrderBy(slot => slot.SlotIndex)
                .Select(slot => MapSlot(slot, context))
                .ToList());
    }

    private EssenceLoadoutSlotDto MapSlot(EssenceLoadoutSlot slot, ResolutionContext context)
    {
        var definition = slot.PlayerEssence is null
            ? null
            : _essenceDefinitions.GetById(slot.PlayerEssence.EssenceDefinitionId);

        return new(
            slot.SlotIndex,
            slot.PlayerEssenceId,
            slot.PlayerEssence?.EssenceDefinitionId,
            definition?.Name,
            definition is null || slot.PlayerEssence is null
                ? null
                : PlayerEssenceDefinitionDtoMapper.Map(definition, slot.PlayerEssence, context.Mapper));
    }
}

public sealed class GatheringProfessionOverviewDto
{
    public ProfessionType ProfessionType { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int ExperienceUntilNextLevel { get; set; }
}
