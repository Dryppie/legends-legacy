using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Dungeons;
using Application.UseCases.Dungeons.Dtos;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class DungeonDtoMappingTests
{
    [Fact]
    public void Dungeon_run_mapping_composes_state_dtos()
    {
        var run = new DungeonRun
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            DungeonDefinitionId = "test_dungeon",
            DungeonDefinitionName = "Test Dungeon",
            CurrentRoomIndex = 0,
            PendingRewards =
            [
                new RunReward
                {
                    ItemId = "ore",
                    Name = "Ore",
                    Quantity = 2
                }
            ],
            Rooms =
            [
                new RoomInstance
                {
                    Id = Guid.NewGuid(),
                    RoomIndex = 0,
                    Type = RoomType.Combat,
                    Status = RoomInstanceStatus.Completed,
                    EncounterIds = ["enemy"]
                },
                new RoomInstance
                {
                    Id = Guid.NewGuid(),
                    RoomIndex = 1,
                    Type = RoomType.RestSite
                }
            ],
            State = new DungeonRunState
            {
                CurrentSection = 2,
                TotalSections = 4,
                MapNodes =
                [
                    new DungeonMapNode
                    {
                        Id = "combat",
                        RoomIndex = 0,
                        Depth = 0,
                        Section = 1,
                        NextRoomIndexes = [1]
                    },
                    new DungeonMapNode
                    {
                        Id = "rest-site",
                        RoomIndex = 1,
                        Depth = 1,
                        Section = 1
                    }
                ],
                PendingLoot = new DungeonLootBag
                {
                    Cinders = 25,
                    Items = new Dictionary<string, int> { ["ore"] = 2 }
                },
                CurrentBossModifiers =
                [
                    new DungeonBossModifier
                    {
                        Id = "power",
                        AttributeType = AttributeType.Power,
                        ModifierType = ModifierType.Additive,
                        Amount = 5
                    }
                ],
                FailureAnalysis = new DungeonFailureAnalysis
                {
                    Location = "Test Room",
                    Section = 2,
                    LostPendingLoot = new DungeonLootBag { Soulstones = 3 }
                }
            }
        };

        var dto = CreateMapper().Map<DungeonRunDto>(run);

        Assert.Equal(2, dto.TotalRooms);
        Assert.Equal(2, dto.State.CurrentSection);
        Assert.Equal(4, dto.State.TotalSections);
        Assert.Equal(25, dto.State.PendingLoot.Cinders);
        Assert.Equal(2, dto.State.PendingLoot.Items["ore"]);
        Assert.Equal("Power", Assert.Single(dto.State.CurrentBossModifiers).AttributeType);
        Assert.Equal("Additive", dto.State.CurrentBossModifiers[0].ModifierType);
        Assert.Equal(3, dto.State.FailureAnalysis?.LostPendingLoot.Soulstones);
        Assert.Equal("rest-site", dto.State.MapNodes[1].Id);
        Assert.Equal("ore", Assert.Single(dto.PendingRewards).ItemId);
    }

    [Fact]
    public void Dungeon_supporting_dtos_use_their_mapping_profiles()
    {
        var mapper = CreateMapper();
        var now = DateTimeOffset.UtcNow;

        var record = mapper.Map<DungeonRecordDto>(new DungeonCompletionRecord
        {
            FirstCompletedAt = now.AddDays(-2),
            LastCompletedAt = now,
            CompletionCount = 4
        });
        var leaderboardEntry = mapper.Map<DungeonRecordEntryDto>(
            new DungeonCompletionLeaderboardEntry(
                Guid.NewGuid(),
                "Hero",
                "test_dungeon",
                now.AddDays(-2),
                now,
                4));
        var mastery = mapper.Map<DungeonMasteryDto>(
            new DungeonMasterySnapshot("test_dungeon", 120, 3, 200, 4));
        var requirement = mapper.Map<DungeonEntryRequirementDto>(
            new DungeonEntryRequirementResult("sigil", "Sigil", 1, 2));
        var reward = mapper.Map<DungeonPreviewRewardDto>(
            new DungeonPreviewReward(
                new ItemBase { Id = "ore", Name = "Ore" },
                "Material",
                "Completion"));
        var actionResponse = mapper.Map<ExecuteDungeonActionResponseDto>(
            new ExecuteDungeonActionResult
            {
                Run = new DungeonRun(),
                Outcome = DungeonActionOutcome.EventResolved,
                Message = "Invalid"
            });

        Assert.True(record.HasCleared);
        Assert.Equal(4, record.TotalClears);
        Assert.Equal("Hero", leaderboardEntry.CharacterName);
        Assert.Equal(now, leaderboardEntry.LastClearedAt);
        Assert.Equal(120, mastery.Experience);
        Assert.Equal(2, requirement.OwnedAmount);
        Assert.Equal("ore", reward.Id);
        Assert.Equal("Ore", reward.ItemBase.Name);
        Assert.Equal(DungeonActionOutcomeDto.EventResolved, actionResponse.Outcome);
        Assert.Equal("Invalid", actionResponse.Message);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            options => options.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }
}
