using Domain.Models.Combat;
using LegendsLegacy.Balance;

namespace EssenceSystem.Tests;

public sealed class WorldTowerFailureAnalyzerTests
{
    [Fact]
    public void Timeout_with_material_guardian_regeneration_is_observed_as_sustain_dominance()
    {
        var result = new CombatResult
        {
            Outcome = BattleOutcome.Draw,
            Duration = 100
        };
        var friendly = new[]
        {
            new EntityStats(
                "player-1",
                "Player 1",
                [],
                DamageDone: 100,
                Team: "Friendly",
                Health: 100,
                MaxHealth: 100)
        };
        var hostile = new[]
        {
            new EntityStats(
                "guardian",
                "Guardian",
                [],
                HealthRegenerated: 30,
                Team: "Hostile",
                Health: 100,
                MaxHealth: 100)
        };

        var diagnostic = WorldTowerContentAnalyzer.AnalyzeFailure(
            result,
            100,
            friendly,
            hostile,
            "guardian");

        Assert.Equal(WorldTowerTerminalFailure.Timeout, diagnostic.TerminalFailure);
        Assert.Equal(WorldTowerObservedFailureMode.BossSustainDominance, diagnostic.PrimaryObservedFailureMode);
        Assert.Equal(0.65, diagnostic.Confidence);
        Assert.Null(diagnostic.AuthoritativeMechanicCause);
        Assert.Contains(diagnostic.Evidence, evidence =>
            evidence.Metric == "guardian_self_sustain_to_friendly_damage"
            && evidence.ObservedValue == 0.3);
    }

    [Fact]
    public void Timeout_with_material_guardian_ability_healing_is_observed_as_sustain_dominance()
    {
        var result = new CombatResult
        {
            Outcome = BattleOutcome.Draw,
            Duration = 100
        };
        var friendly = new[]
        {
            new EntityStats(
                "player-1",
                "Player 1",
                [],
                DamageDone: 100,
                Team: "Friendly",
                Health: 100,
                MaxHealth: 100)
        };
        var hostile = new[]
        {
            new EntityStats(
                "guardian",
                "Guardian",
                [],
                HealingDone: 30,
                Team: "Hostile",
                Health: 100,
                MaxHealth: 100)
        };

        var diagnostic = WorldTowerContentAnalyzer.AnalyzeFailure(
            result,
            100,
            friendly,
            hostile,
            "guardian");

        Assert.Equal(WorldTowerObservedFailureMode.BossSustainDominance, diagnostic.PrimaryObservedFailureMode);
        Assert.Contains(diagnostic.Evidence, evidence =>
            evidence.Metric == "guardian_self_sustain_to_friendly_damage"
            && evidence.ObservedValue == 0.3);
    }

    [Fact]
    public void Focused_member_death_is_observed_without_claiming_an_authoritative_cause()
    {
        var result = new CombatResult
        {
            Outcome = BattleOutcome.Defeat,
            Duration = 60
        };
        var friendly = new[]
        {
            new EntityStats(
                "player-1",
                "Player 1",
                [],
                Team: "Friendly",
                Health: 0,
                MaxHealth: 100,
                TargetedAttacks: 80,
                AttentionSharePercent: 80,
                Deaths: 1),
            new EntityStats(
                "player-2",
                "Player 2",
                [],
                Team: "Friendly",
                Health: 0,
                MaxHealth: 100,
                TargetedAttacks: 20,
                AttentionSharePercent: 20,
                Deaths: 1)
        };

        var diagnostic = WorldTowerContentAnalyzer.AnalyzeFailure(
            result,
            100,
            friendly,
            [],
            "guardian");

        Assert.Equal(WorldTowerTerminalFailure.PartyDefeated, diagnostic.TerminalFailure);
        Assert.Equal(WorldTowerObservedFailureMode.PrimaryTargetCollapse, diagnostic.PrimaryObservedFailureMode);
        Assert.Contains(WorldTowerObservedFailureMode.PartyAttrition, diagnostic.ContributingConditions);
        Assert.Null(diagnostic.AuthoritativeMechanicCause);
        Assert.Contains(diagnostic.Evidence, evidence =>
            evidence.Metric == "highest_attention_share"
            && evidence.EntityId == "player-1");
    }

    [Fact]
    public void Victory_has_no_failure_observation()
    {
        var diagnostic = WorldTowerContentAnalyzer.AnalyzeFailure(
            new CombatResult { Outcome = BattleOutcome.Victory, Duration = 40 },
            100,
            [],
            [],
            "guardian");

        Assert.Equal(WorldTowerFailureDiagnosticSnapshot.Success, diagnostic);
    }

    [Fact]
    public void Timeout_with_unresolved_adds_is_observed_as_add_pressure()
    {
        var result = new CombatResult
        {
            Outcome = BattleOutcome.Draw,
            Duration = 100,
            CompactTelemetry = new CompactCombatTelemetry(
                PeakActiveHostileCombatants: 4,
                FirstAdditionalHostileTick: 20,
                FinalActiveHostileCombatants: 3)
        };

        var diagnostic = WorldTowerContentAnalyzer.AnalyzeFailure(
            result,
            100,
            [new EntityStats("player", "Player", [], DamageDone: 100, Team: "Friendly", Health: 100, MaxHealth: 100)],
            [new EntityStats("guardian", "Guardian", [], Team: "Hostile", Health: 100, MaxHealth: 100)],
            "guardian");

        Assert.Equal(WorldTowerTerminalFailure.Timeout, diagnostic.TerminalFailure);
        Assert.Equal(WorldTowerObservedFailureMode.AddPressure, diagnostic.PrimaryObservedFailureMode);
        Assert.Null(diagnostic.AuthoritativeMechanicCause);
        Assert.Contains(diagnostic.Evidence, evidence =>
            evidence.Metric == "final_additional_hostiles" && evidence.ObservedValue == 2);
    }
}
