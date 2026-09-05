using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Characters.Dtos;
using Domain.Helpers.Constants;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.CharacterActions;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences.Definitions;
using Domain.Models.Guilds;

namespace EssenceSystem.Tests;

public sealed class CharacterOverviewConverterTests
{
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
