using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Characters.Dtos;
using Domain.Helpers.Constants;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.CharacterActions;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences.Definitions;
using Domain.Models.Guilds;
using Domain.Models.Professions;
using Domain.Models.Professions.Gathering;

namespace EssenceSystem.Tests;

public sealed class CharacterOverviewConverterTests
{
    [Fact]
    public void Convert_UsesHighestCraftingProfessionForLevelAndExperience()
    {
        var character = new Character
        {
            Professions =
            [
                new Profession
                {
                    ProfessionType = ProfessionType.Mining,
                    Level = 20,
                    Experience = 99
                },
                new Profession
                {
                    ProfessionType = ProfessionType.Crafting,
                    Level = 4,
                    Experience = 75
                },
                new Profession
                {
                    ProfessionType = (ProfessionType)3,
                    Level = 5,
                    Experience = 42
                }
            ]
        };

        var result = CreateConverter()
            .Convert(character, null!, null!);

        Assert.Equal(5, result.CraftingLevel);
        Assert.Equal(42, result.CraftingExperience);
        Assert.Equal(
            EntityLevelConstants.XP_REQUIRED(5),
            result.CraftingExperienceUntilNextLevel);
    }

    [Fact]
    public void Convert_DefaultsToLevelOneCraftingProgress()
    {
        var result = CreateConverter()
            .Convert(new Character(), null!, null!);

        Assert.Equal(1, result.CraftingLevel);
        Assert.Equal(0, result.CraftingExperience);
        Assert.Equal(
            EntityLevelConstants.XP_REQUIRED(1),
            result.CraftingExperienceUntilNextLevel);
    }

    [Fact]
    public void Convert_ProjectsAllGatheringProfessionsWithTheirCanonicalCurve()
    {
        var character = new Character
        {
            Professions =
            [
                new Profession
                {
                    ProfessionType = ProfessionType.Mining,
                    Level = 12,
                    Experience = 345
                },
                new Profession
                {
                    ProfessionType = ProfessionType.Skinning,
                    Level = 100,
                    Experience = 0
                }
            ]
        };

        var result = CreateConverter()
            .Convert(character, null!, null!);

        Assert.Collection(
            result.GatheringProfessions,
            mining =>
            {
                Assert.Equal(ProfessionType.Mining, mining.ProfessionType);
                Assert.Equal(12, mining.Level);
                Assert.Equal(345, mining.Experience);
                Assert.Equal(GatheringProfessionProgression.GetRequiredExperience(12), mining.ExperienceUntilNextLevel);
            },
            woodcutting =>
            {
                Assert.Equal(ProfessionType.Woodcutting, woodcutting.ProfessionType);
                Assert.Equal(1, woodcutting.Level);
                Assert.Equal(0, woodcutting.Experience);
                Assert.Equal(GatheringProfessionProgression.GetRequiredExperience(1), woodcutting.ExperienceUntilNextLevel);
            },
            skinning =>
            {
                Assert.Equal(ProfessionType.Skinning, skinning.ProfessionType);
                Assert.Equal(100, skinning.Level);
                Assert.Equal(0, skinning.ExperienceUntilNextLevel);
            });
    }

    [Fact]
    public void Convert_ProjectsDefaultThreatForExistingCharactersWithoutAStoredAttribute()
    {
        var result = CreateConverter()
            .Convert(new Character(), null!, null!);

        Assert.Equal(
            100,
            result.BaseAttributes.Single(attribute => attribute.AttributeType == AttributeType.Threat).Value);
        Assert.Equal(
            100,
            result.BaseCombatAttributes.Single(attribute => attribute.AttributeType == AttributeType.Threat).Value);
    }

    [Fact]
    public void CharacterGuildDto_MapsPublicGuildIdentity()
    {
        var guildId = Guid.NewGuid();
        var result = CharacterGuildDto.From(new GuildMember
        {
            GuildId = guildId,
            Guild = new Guild
            {
                Id = guildId,
                Name = "The Wayfinders",
                Tag = "WAY"
            }
        });

        Assert.NotNull(result);
        Assert.Equal(guildId, result.Id);
        Assert.Equal("The Wayfinders", result.Name);
        Assert.Equal("WAY", result.Tag);
    }

    [Fact]
    public void CharacterGuildDto_ReturnsNullForPlayerWithoutGuild()
    {
        Assert.Null(CharacterGuildDto.From(null));
    }

    [Theory]
    [InlineData(-10, true)]
    [InlineData(-21, false)]
    public void Convert_UsesIdleActionActivityForOnlineAndLastSeen(
        int activityOffsetMinutes,
        bool expectedOnline)
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var lastSeenAt = now.AddMinutes(activityOffsetMinutes);
        var character = new Character
        {
            CharacterAction = new CharacterAction
            {
                UpdatedAt = lastSeenAt
            }
        };

        var result = new CharacterOverviewConverter(
                new EmptyEssenceDefinitions(),
                new FixedTimeProvider(now))
            .Convert(character, null!, null!);

        Assert.Equal(expectedOnline, result.IsOnline);
        Assert.Equal(lastSeenAt, result.LastSeenAt);
    }

    [Fact]
    public void Convert_IncludesUnlockedEmptyEssenceSlotsInDefaultLoadout()
    {
        var loadout = new Domain.Models.Essences.EssenceLoadout
        {
            Id = Guid.NewGuid(),
            Name = "Default"
        };
        loadout.Slots.Add(new Domain.Models.Essences.EssenceLoadoutSlot
        {
            SlotIndex = 0
        });
        var character = new Character
        {
            Level = 10,
            EssenceLoadouts = [loadout]
        };

        var result = CreateConverter().Convert(character, null!, null!);

        Assert.NotNull(result.EssenceLoadout);
        Assert.Equal([0, 1], result.EssenceLoadout.Slots.Select(slot => slot.SlotIndex));
        Assert.Null(result.EssenceLoadout.Slots[1].PlayerEssenceId);
    }

    private static CharacterOverviewConverter CreateConverter() =>
        new(new EmptyEssenceDefinitions());

    private sealed class EmptyEssenceDefinitions : IEssenceDefinitionRepository
    {
        public IReadOnlyList<EssenceDefinition> GetAll() => [];

        public IReadOnlyList<AbilitySpec> GetAllAbilities() => [];

        public EssenceDefinition? GetById(string essenceDefinitionId) => null;

        public AbilitySpec? GetAbilityById(string abilityId) => null;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
