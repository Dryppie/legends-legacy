using System.Text.Json;
using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.Colosseum.Tournaments;
using Application.UseCases.Raids.Dtos;
using Application.UseCases.WorldTower.Dtos;
using AutoMapper;
using Domain.Models.Combat;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class CombatSummaryTransportTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Ordinary_combat_results_keep_normal_stats_without_serializing_internal_style_diagnostics()
    {
        var result = new CombatResult
        {
            Duration = 12,
            Outcome = BattleOutcome.Victory,
            ExperienceGained = 75,
            EntityStats = [new("hero", "Hero", [], DamageDone: 100, HealingDone: 25, BarrierGenerated: 50)],
            CombatStyles = [new() { EntityId = "hero", CombatStyleId = "bastion", HealingConverted = 75 }]
        };
        var mapper = new MapperConfiguration(configuration => configuration.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

        var dto = mapper.Map<CombatResultDto>(result);
        using var response = JsonDocument.Parse(JsonSerializer.Serialize(dto, JsonOptions));
        using var storedResult = JsonDocument.Parse(JsonSerializer.Serialize(result, JsonOptions));

        foreach (var contract in new[] { response.RootElement, storedResult.RootElement })
        {
            Assert.False(contract.TryGetProperty("combatStyles", out _));
            Assert.Equal(75, contract.GetProperty("experienceGained").GetInt32());
            var stats = contract.GetProperty("entityStats")[0];
            Assert.Equal(100, stats.GetProperty("damageDone").GetInt32());
            Assert.Equal(25, stats.GetProperty("healingDone").GetInt32());
            Assert.Equal(50, stats.GetProperty("barrierGenerated").GetInt32());
        }
        Assert.Equal(75, Assert.Single(result.CombatStyles).HealingConverted);
    }

    [Fact]
    public void Old_stored_results_ignore_retired_style_metadata_and_preserve_combat_outcome_and_rewards()
    {
        const string oldResult = """
            {
              "duration": 12, "outcome": 0, "experienceGained": 75,
              "entityStats": [{ "entityId": "hero", "entityName": "Hero", "abilities": [], "damageDone": 100 }],
              "combatStyles": [{ "entityId": "hero", "combatStyleId": "bastion", "healingConverted": 75 }]
            }
            """;

        var result = JsonSerializer.Deserialize<CombatResult>(oldResult, JsonOptions)!;
        Assert.Equal(12, result.Duration);
        Assert.Equal((BattleOutcome)0, result.Outcome);
        Assert.Equal(75, result.ExperienceGained);
        Assert.Equal(100, Assert.Single(result.EntityStats).DamageDone);
        Assert.Empty(result.CombatStyles);
        using var rewritten = JsonDocument.Parse(JsonSerializer.Serialize(result, JsonOptions));
        Assert.False(rewritten.RootElement.TryGetProperty("combatStyles", out _));
    }

    [Theory]
    [InlineData(typeof(TournamentPlaybackFrameDto))]
    [InlineData(typeof(RaidPlaybackFrameDto))]
    [InlineData(typeof(TowerPlaybackBundleFrameDto))]
    public void Compact_playback_frames_accept_old_style_metadata_without_retaining_it(Type frameType)
    {
        const string oldFrame = """
            {
              "sequence": 2, "tick": 20, "isKeyframe": true, "isFinal": true, "outcome": 0,
              "entityStates": [{ "entityIndex": 0, "health": 80, "barrier": 20 }],
              "entityTotals": [{ "entityIndex": 0, "damageDone": 100, "healingDone": 25, "barrierGenerated": 50 }],
              "abilityTotals": [{ "abilityIndex": 0, "uses": 2, "totalDamage": 100, "totalHealing": 25, "totalBarrier": 50 }],
              "combatStyles": [{ "entityId": "hero", "combatStyleId": "bastion", "healingConverted": 75 }]
            }
            """;

        var frame = JsonSerializer.Deserialize(oldFrame, frameType, JsonOptions);
        Assert.NotNull(frame);
        using var rewritten = JsonDocument.Parse(JsonSerializer.Serialize(frame, frameType, JsonOptions));
        var contract = rewritten.RootElement;

        Assert.False(contract.TryGetProperty("combatStyles", out _));
        Assert.Equal(20, contract.GetProperty("tick").GetInt32());
        Assert.True(contract.GetProperty("isFinal").GetBoolean());
        Assert.Equal(80, contract.GetProperty("entityStates")[0].GetProperty("health").GetInt32());
        Assert.Equal(100, contract.GetProperty("entityTotals")[0].GetProperty("damageDone").GetInt32());
        Assert.Equal(25, contract.GetProperty("abilityTotals")[0].GetProperty("totalHealing").GetInt32());
        Assert.Equal(50, contract.GetProperty("abilityTotals")[0].GetProperty("totalBarrier").GetInt32());
    }
}
