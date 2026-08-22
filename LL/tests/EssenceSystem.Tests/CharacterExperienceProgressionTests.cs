using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Characters.Events;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Progression;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Professions;
using Domain.Models.Professions.Gathering;
using Domain.Helpers.Constants;
using Domain.Helpers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Services.LL.Levels;
using Services.LL.Regions;
using Services.LL.Dungeons;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class CharacterExperienceProgressionTests
{
    [Theory]
    [InlineData(1, 200)]
    [InlineData(5, 2_425)]
    [InlineData(10, 9_400)]
    [InlineData(20, 37_300)]
    [InlineData(30, 83_800)]
    [InlineData(40, 148_900)]
    [InlineData(45, 188_425)]
    [InlineData(60, 334_900)]
    [InlineData(75, 523_225)]
    [InlineData(100, 930_100)]
    public void Json_curve_matches_progression_milestones(int level, int expectedExperience)
    {
        var provider = CreateProgressionProvider();

        Assert.Equal(expectedExperience, provider.GetRequiredExperience(level));
    }

    [Fact]
    public void Json_curve_is_strictly_increasing_and_continues_beyond_level_100()
    {
        var provider = CreateProgressionProvider();
        var requirements = Enumerable.Range(1, 10_000)
            .Select(provider.GetRequiredExperience)
            .ToArray();

        Assert.All(requirements.Zip(requirements.Skip(1)), pair => Assert.True(pair.First < pair.Second));
        Assert.Equal(948_800, provider.GetRequiredExperience(101));
        Assert.Equal(930_000_000_100, provider.GetRequiredExperience(100_000));
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetRequiredExperience(0));
    }

    [Fact]
    public void Curve_uses_upward_integer_rounding_and_checked_arithmetic()
    {
        var settings = new CharacterExperienceCurveSettings
        {
            BaseExperience = 101,
            LinearExperiencePerLevel = 0,
            QuadraticExperiencePerLevelSquared = 1,
            RoundingIncrement = 25
        };

        Assert.Equal(125, CharacterExperienceCurve.CalculateRequiredExperience(1, settings));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CharacterExperienceCurve.CalculateRequiredExperience(0, settings));

        settings.QuadraticExperiencePerLevelSquared = int.MaxValue;
        Assert.Throws<OverflowException>(() =>
            CharacterExperienceCurve.CalculateRequiredExperience(int.MaxValue, settings));
    }

    [Fact]
    public async Task Leveling_continues_beyond_level_100_and_preserves_overflow()
    {
        var publisher = new RecordingPublisher();
        var service = new LevelingService(publisher, new FlatProgressionProvider());
        var character = new Character { Level = 100, Experience = 250 };

        await service.UpdateCharacterLevel(character, CancellationToken.None);

        Assert.Equal(102, character.Level);
        Assert.Equal(50, character.Experience);
        Assert.Equal(2, publisher.Notifications.Count);
        var levelUp = Assert.IsType<CharacterLevelUpEvent>(publisher.Notifications[^1]);
        Assert.Equal(100, levelUp.ExperienceUntilNextLevel);
    }

    [Fact]
    public async Task Leveling_supports_multiple_levels_and_preserves_non_maximum_overflow()
    {
        var service = new LevelingService(new RecordingPublisher(), new FourLevelProgressionProvider());
        var character = new Character { Level = 1, Experience = 350 };

        await service.UpdateCharacterLevel(character, CancellationToken.None);

        Assert.Equal(3, character.Level);
        Assert.Equal(50, character.Experience);
    }

    [Fact]
    public async Task Leveling_increases_power_and_visible_health()
    {
        var service = new LevelingService(new RecordingPublisher(), new FlatProgressionProvider());
        var character = new Character
        {
            Level = 1,
            Experience = 100,
            BaseAttributes =
            [
                new EntityAttribute { AttributeType = AttributeType.Power, Value = 10 },
                new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 140 }
            ]
        };

        await service.UpdateCharacterLevel(character, CancellationToken.None);

        Assert.Equal(10.25f, character.BaseAttributes.Single(x => x.AttributeType == AttributeType.Power).Value);
        Assert.Equal(160, character.BaseAttributes.Single(x => x.AttributeType == AttributeType.MaxHealth).Value);
    }

    [Fact]
    public void Character_level_attributes_follow_the_same_deterministic_curve_at_any_level()
    {
        var attributes = EntityBaseAttributeHelper
            .CreateEntityAttributesForLevel(Guid.Empty, 500)
            .ToDictionary(attribute => attribute.AttributeType, attribute => attribute.Value);

        Assert.Equal(134.75f, attributes[AttributeType.Power]);
        Assert.Equal(10_120, attributes[AttributeType.MaxHealth]);
        Assert.Equal(100, attributes[AttributeType.Threat]);
    }

    [Fact]
    public void Each_level_up_uses_the_active_equipment_attribute_exchange_rates()
    {
        var internalBudget =
            EntityBaseAttributeHelper.PowerPerCharacterLevel
            * EquipmentStatBudgetCatalog.Get(AttributeType.Power, tier: 1).CostPerPoint
            + EntityBaseAttributeHelper.MaxHealthPerCharacterLevel
            * EquipmentStatBudgetCatalog.Get(AttributeType.MaxHealth, tier: 1).CostPerPoint;

        Assert.Equal(9.325d, internalBudget, precision: 6);
        Assert.Equal(0.9325d, internalBudget / 10d, precision: 6);
    }

    [Fact]
    public async Task Multi_level_gain_recomputes_attributes_from_the_final_level()
    {
        var service = new LevelingService(new RecordingPublisher(), new FlatProgressionProvider());
        var character = new Character
        {
            Level = 1,
            Experience = 300,
            BaseAttributes =
            [
                new EntityAttribute { AttributeType = AttributeType.Power, Value = 10 },
                new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 140 }
            ]
        };

        await service.UpdateCharacterLevel(character, CancellationToken.None);

        Assert.Equal(4, character.Level);
        Assert.Equal(10.75f, character.BaseAttributes.Single(x => x.AttributeType == AttributeType.Power).Value);
        Assert.Equal(200, character.BaseAttributes.Single(x => x.AttributeType == AttributeType.MaxHealth).Value);
    }

    [Fact]
    public async Task Profession_leveling_remains_independent_from_the_character_curve()
    {
        var service = new LevelingService(new RecordingPublisher(), new FlatProgressionProvider());
        var profession = new Profession
        {
            Level = 1,
            Experience = EntityLevelConstants.XP_REQUIRED(1)
        };

        await service.UpdateProfessionLevel(profession, CancellationToken.None);

        Assert.Equal(2, profession.Level);
        Assert.Equal(0, profession.Experience);
    }

    [Theory]
    [InlineData(1, 474)]
    [InlineData(20, 189_600)]
    [InlineData(50, 1_185_000)]
    [InlineData(99, 4_645_674)]
    public void Gathering_professions_use_the_quadratic_360_day_curve(
        int level,
        int expectedExperience)
    {
        Assert.Equal(
            expectedExperience,
            GatheringProfessionProgression.GetRequiredExperience(level));
    }

    [Fact]
    public void Gathering_level_one_hundred_requires_the_planned_total_experience()
    {
        var totalExperience = GatheringProfessionProgression.GetCumulativeExperienceForLevel(100);
        var idealDays = totalExperience / 432_000d;

        Assert.Equal(155_637_900, totalExperience);
        Assert.Equal(360.27d, idealDays, precision: 2);
    }

    [Fact]
    public async Task Gathering_professions_stop_at_level_one_hundred()
    {
        var service = new LevelingService(new RecordingPublisher(), new FlatProgressionProvider());
        var profession = new Profession
        {
            ProfessionType = ProfessionType.Mining,
            Level = 99,
            Experience = GatheringProfessionProgression.GetRequiredExperience(99) + 500
        };

        await service.UpdateProfessionLevel(profession, CancellationToken.None);

        Assert.Equal(100, profession.Level);
        Assert.Equal(0, profession.Experience);
    }

    [Fact]
    public void Dungeon_rewards_scale_by_tier_and_room_type_independently_from_creatures()
    {
        var provider = new JsonDungeonRewardBalanceProvider(
            CreateConfiguration(),
            FindApiRoot(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal((2_500, 100), ToTuple(provider.GetEncounterReward(1, RoomType.Combat)));
        Assert.Equal((5_250, 210), ToTuple(provider.GetEncounterReward(2, RoomType.MiniBoss)));
        Assert.Equal((12_250, 490), ToTuple(provider.GetEncounterReward(3, RoomType.Boss)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            provider.GetEncounterReward(0, RoomType.Combat));
    }

    private static (int Experience, int Cinders) ToTuple(
        Application.Interfaces.Services.LL.Dungeons.DungeonEncounterReward reward) =>
        (reward.Experience, reward.Cinders);

    private static JsonCharacterExperienceProgressionProvider CreateProgressionProvider()
    {
        var apiRoot = FindApiRoot();
        return new JsonCharacterExperienceProgressionProvider(
            CreateConfiguration(),
            apiRoot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data",
                ["Combat:IdleProgression:EncounterCadenceSeconds"] = "10"
            })
            .Build();

    private static string FindApiRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidates = new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
            };

            var match = candidates.FirstOrDefault(candidate =>
                File.Exists(Path.Combine(candidate, "Data", "progression", "character-experience.json")));
            if (match is not null)
            {
                return match;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate API.LL progression and world data.");
    }

    private sealed class FlatProgressionProvider : ICharacterExperienceProgressionProvider
    {
        public long GetRequiredExperience(int level) => 100;
    }

    private sealed class FourLevelProgressionProvider : ICharacterExperienceProgressionProvider
    {
        public long GetRequiredExperience(int level) => level switch
        {
            1 => 100,
            2 => 200,
            3 => 300,
            4 => 400,
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };
    }

    private sealed class RecordingPublisher : IPublisher
    {
        public List<object> Notifications { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Publish((object)notification, cancellationToken);
    }
}
