using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Prophecies;
using Application.Interfaces.Services.LL.Dungeons;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Prophecies.Dtos;
using AutoMapper;
using Domain.Models.Prophecies;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class ProphecyDtoMappingTests
{
    [Theory]
    [InlineData(ProphecyObjectiveType.KillCreatures, ProphecyGuidanceDestination.WorldCombat, "Fight Encounters")]
    [InlineData(ProphecyObjectiveType.KillDifferentCreatureTypes, ProphecyGuidanceDestination.WorldCombat, "Fight Encounters")]
    [InlineData(ProphecyObjectiveType.WinEncounters, ProphecyGuidanceDestination.WorldCombat, "Fight Encounters")]
    [InlineData(ProphecyObjectiveType.ClearDungeonRooms, ProphecyGuidanceDestination.Dungeons, "Run Dungeons")]
    [InlineData(ProphecyObjectiveType.CompleteDungeons, ProphecyGuidanceDestination.Dungeons, "Run Dungeons")]
    [InlineData(ProphecyObjectiveType.GainEssenceXp, ProphecyGuidanceDestination.Essences, "Train Essences")]
    [InlineData(ProphecyObjectiveType.AbsorbEssence, ProphecyGuidanceDestination.SoulArchive, "Open Archive")]
    [InlineData(ProphecyObjectiveType.GatherResources, ProphecyGuidanceDestination.Gathering, "Gather Resources")]
    [InlineData(ProphecyObjectiveType.TemperItems, ProphecyGuidanceDestination.Crafting, "Temper Gear")]
    [InlineData(ProphecyObjectiveType.SpendPotential, ProphecyGuidanceDestination.Crafting, "Temper Gear")]
    [InlineData(ProphecyObjectiveType.TreasureProgress, ProphecyGuidanceDestination.Dungeons, "Seek Treasure")]
    [InlineData(ProphecyObjectiveType.MeaningfulDefeatThenWins, ProphecyGuidanceDestination.WorldCombat, "Return To Battle")]
    public void ProphecyInstanceDto_maps_server_owned_guidance(
        string objectiveType,
        string expectedDestination,
        string expectedActionLabel)
    {
        var dto = CreateMapper().Map<ProphecyInstanceDto>(CreateInstance(objectiveType));

        Assert.Equal(expectedDestination, dto.Guidance.Destination);
        Assert.Equal(expectedActionLabel, dto.Guidance.ActionLabel);
        Assert.False(string.IsNullOrWhiteSpace(dto.Guidance.Hint));
    }

    [Fact]
    public void ProphecyInstanceDto_uses_snapshotted_gathering_profession_in_guidance()
    {
        var instance = CreateInstance(ProphecyObjectiveType.GatherResources);
        instance.ProphecyDefinition!.ObjectiveParameterJson = "{\"requiredProfession\":\"Woodcutting\"}";
        instance.ObjectiveParameterSnapshotJson = "{\"requiredProfession\":\"Mining\"}";

        var dto = CreateMapper().Map<ProphecyInstanceDto>(instance);

        Assert.Equal("Go Mining", dto.Guidance.ActionLabel);
        Assert.Contains("Only Mining", dto.Guidance.Hint);
    }

    [Fact]
    public void ProphecyInstanceDto_explains_minimum_enemy_count_from_snapshot()
    {
        var instance = CreateInstance(ProphecyObjectiveType.WinEncounters);
        instance.ObjectiveParameterSnapshotJson = "{\"minimumEnemyCount\":3}";

        var dto = CreateMapper().Map<ProphecyInstanceDto>(instance);

        Assert.Equal("Fight Groups", dto.Guidance.ActionLabel);
        Assert.Contains("at least 3 enemies", dto.Guidance.Hint);
    }

    [Fact]
    public void ProphecyInstanceDto_explains_that_every_defeated_creature_counts()
    {
        var dto = CreateMapper().Map<ProphecyInstanceDto>(
            CreateInstance(ProphecyObjectiveType.KillCreatures));

        Assert.Equal(
            "Every creature you defeat in combat counts toward this prophecy.",
            dto.Guidance.Hint);
    }

    [Fact]
    public void DungeonSigilAssemblyResponseDto_maps_all_result_fields()
    {
        var result = new DungeonSigilAssemblyResult(
            "goblin_mines.grade_1",
            "item.sigil.goblin",
            "Goblin Sigil",
            3,
            75);

        var dto = CreateMapper().Map<DungeonSigilAssemblyResponseDto>(result);

        Assert.Equal(result.DungeonId, dto.DungeonId);
        Assert.Equal(result.SigilItemId, dto.SigilItemId);
        Assert.Equal(result.SigilName, dto.SigilName);
        Assert.Equal(result.InventoryQuantity, dto.InventoryQuantity);
        Assert.Equal(result.SigilFragmentsRemaining, dto.SigilFragmentsRemaining);
    }

    private static PlayerProphecyInstance CreateInstance(string objectiveType) =>
        new()
        {
            Id = Guid.NewGuid(),
            PlayerId = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            ProphecyDefinitionId = $"test.{objectiveType}",
            ProphecyDefinition = new ProphecyDefinition
            {
                Id = $"test.{objectiveType}",
                Title = "Test Prophecy",
                FlavorText = "Test flavor",
                ObjectiveText = "Complete {target} actions.",
                ObjectiveType = objectiveType
            },
            ObjectiveParameterSnapshotJson = "{}",
            RewardSnapshotJson = "{}",
            TargetValue = 5
        };

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }
}
