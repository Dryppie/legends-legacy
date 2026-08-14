using Domain.Models.Attributes;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class TournamentOvertimeCombatTests
{
    [Fact]
    public void Power_ramp_applies_first_ten_percent_step_ten_seconds_into_overtime()
    {
        var baselineAtTenSeconds = RunCombat(maxTicks: 100, enableOvertimePowerRamp: false);
        var overtimeAtTenSeconds = RunCombat(maxTicks: 100, enableOvertimePowerRamp: true);
        Assert.Equal(baselineAtTenSeconds, overtimeAtTenSeconds);

        var baselineAfterFirstStep = RunCombat(maxTicks: 101, enableOvertimePowerRamp: false);
        var overtimeAfterFirstStep = RunCombat(maxTicks: 101, enableOvertimePowerRamp: true);
        Assert.True(overtimeAfterFirstStep > baselineAfterFirstStep);
    }

    private static int RunCombat(int maxTicks, bool enableOvertimePowerRamp)
    {
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: maxTicks,
                BasicAttackIntervalTicks: 1,
                RandomSeed: 42,
                OvertimeStartsAtTick: enableOvertimePowerRamp ? 0 : null,
                OvertimePowerIncreaseIntervalTicks: 100,
                OvertimePowerIncreasePercent: 10));

        var result = engine.Run([friendly], [hostile]);
        return result.EntityStats.Single(stats => stats.EntityId == friendly.Id).DamageDone;
    }

    private static RuntimeCombatant CreateCombatant(string id, CombatTeam team) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 1_000_000,
                [AttributeType.Power] = 1_000
            },
            []);
}
