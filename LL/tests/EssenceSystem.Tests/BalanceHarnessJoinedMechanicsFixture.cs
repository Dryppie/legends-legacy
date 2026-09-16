using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

internal static class BalanceHarnessJoinedMechanicsFixture
{
    internal static BossMechanicCore Core(params string[] ids) => new(HarnessJson.Hash(ids), "basic-attack", ids,
        ids.Select(id => "Effect:" + id).ToArray(), "Synthetic structural hypothesis.");

    internal static BossGenerationMechanics Mechanics(BossDiscoveryInputs input) => F.Mechanics(input) with {
        Cores = [Core("e00", "e01"), Core("e00", "e02"), Core("e02", "e03")],
        Coverage = [new("e00", "attack-enabler", ["Effect:e00"])]
    };
}
