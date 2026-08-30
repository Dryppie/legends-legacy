using Domain.Models.Attributes;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class FastCombatEngineOutcomeTests
{
    [Fact]
    public void Both_teams_dead_results_in_a_draw()
    {
        var friendly = Combatant("friendly", CombatTeam.Friendly);
        var hostile = Combatant("hostile", CombatTeam.Hostile);
        friendly.SetHealth(0);
        hostile.SetHealth(0);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>());

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(BattleOutcome.Draw, result.Outcome);
    }

    private static RuntimeCombatant Combatant(string id, CombatTeam team) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100
            },
            [],
            canBasicAttack: false);
}
