using BalanceHarness;
using Domain.Models.CombatStyles;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class CombatStyleHarnessTests
{
    [Fact]
    public async Task Styles_are_frozen_and_replayed_with_owned_focus_identity_and_compact_metrics()
    {
        var content = new OfflineContent(TestContentPaths.FindApiRoot(), new ThreatAndTankingOptions());
        var suite = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(TestContentPaths.FindApiRoot(),
            "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "combat-styles.json"));
        var resolved = IdleSuite.Resolve(suite with { SamplesPerCell = 1 }, content, new(), 60, 1337);
        var cell = resolved.Cells.First(x => x.Build == "conduit-10");
        var input = cell.Input;
        Assert.Equal(10, input.Character.CombatStyle!.Level);
        Assert.Equal(.10, input.Character.CombatStyle.FocusMasteryBonus, 6);
        Assert.Equal(input.Character.MaterializeEssences().Single(x => x.EssenceDefinitionId == "essence.goblin_warrior").Id,
            input.Character.CombatStyle.FocusPlayerEssenceId);
        var runner = new IdleBattleRunner(content);
        var first = await runner.RunAsync(input);
        var second = await runner.RunAsync(input);
        Assert.Equal(HarnessJson.Hash(first), HarnessJson.Hash(second));
        Assert.Equal(CombatStyleIds.Conduit, Assert.Single(first.Summary.CombatStyles!).CombatStyleId);
        Assert.Null(resolved.Cells.First(x => x.Build == "control").Input.Character.CombatStyle);
    }

    [Fact]
    public void Snapshot_tampering_and_illegal_upgrade_selection_are_rejected()
    {
        var content = new OfflineContent(TestContentPaths.FindApiRoot(), new ThreatAndTankingOptions());
        var suite = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(TestContentPaths.FindApiRoot(),
            "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "combat-styles.json"));
        var input = IdleSuite.Resolve(suite with { SamplesPerCell = 1 }, content, new(), 60, 1337)
            .Cells.First(x => x.Build == "bastion-1").Input;
        Assert.Throws<InvalidDataException>(() => content.Validate(input with
        { Character = input.Character with { CombatStyle = input.Character.CombatStyle! with { Level = 10 } } }));
        Assert.Throws<InvalidDataException>(() => content.Validate(input with
        {
            Character = input.Character with
            {
                CombatStyle = input.Character.CombatStyle! with
                { Tuning = input.Character.CombatStyle.Tuning with { BarrierPerMasteryLevel = .05 } }
            }
        }));
        Assert.Throws<InvalidDataException>(() => content.CreateInput(input.Scenario with
        { CombatStyle = new FixtureCombatStyle("bastion", 1, UpgradeIds: [CombatStyleIds.PreparedWall]) }, 1337, new(), 60));
    }

    [Theory]
    [InlineData(CombatStyleIds.Bastion, CombatStyleIds.PreparedWall, null)]
    [InlineData(CombatStyleIds.Conduit, CombatStyleIds.FullCircuit, "essence.goblin_warrior")]
    public void Mastery_and_opening_tuning_are_frozen_and_validated_with_the_build(
        string styleId, string upgradeId, string? focus)
    {
        var content = new OfflineContent(TestContentPaths.FindApiRoot(), new ThreatAndTankingOptions());
        var suite = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(TestContentPaths.FindApiRoot(),
            "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "combat-styles.json"));
        var scenario = IdleSuite.Resolve(suite with { SamplesPerCell = 1 }, content, new(), 60, 1337)
            .Cells.First(x => x.Build == "bastion-1").Input.Scenario;
        var recipe = new FixtureCombatStyle(styleId, 9, UpgradeIds: [upgradeId],
            FocusEssenceDefinitionId: focus, MasteredUpgradeId: upgradeId);
        var input = content.CreateInput(scenario with { CombatStyle = recipe }, 1337, new(), 60);
        var frozen = input.Character.CombatStyle!;

        Assert.True(frozen.HasMasteredUpgrade(upgradeId));
        if (styleId == CombatStyleIds.Bastion)
            Assert.Equal(.05, frozen.MilestoneTuning.OpeningBarrierFraction);
        else
            Assert.Equal(1, frozen.MilestoneTuning.OpeningCharge);
        content.Validate(input);
        Assert.Throws<InvalidDataException>(() => content.Validate(input with
        {
            Character = input.Character with { CombatStyle = frozen with { MasteredUpgradeId = null } }
        }));
        Assert.Throws<InvalidDataException>(() => content.CreateInput(
            scenario with { CombatStyle = recipe with { Level = 8 } }, 1337, new(), 60));
        Assert.Throws<InvalidDataException>(() => content.CreateInput(
            scenario with { CombatStyle = recipe with { UpgradeIds = [] } }, 1337, new(), 60));
    }

    [Fact]
    public async Task Reprisal_recipe_freezes_absorption_rules_and_replays_deterministically()
    {
        var content = new OfflineContent(TestContentPaths.FindApiRoot(), new ThreatAndTankingOptions());
        var suite = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(TestContentPaths.FindApiRoot(),
            "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "combat-styles.json"));
        var scenario = IdleSuite.Resolve(suite with { SamplesPerCell = 1 }, content, new(), 60, 1337)
            .Cells.First(x => x.Build == "bastion-1").Input.Scenario;
        var input = content.CreateInput(scenario with
        {
            CombatStyle = new FixtureCombatStyle(CombatStyleIds.Bastion, 10, RefinementId: CombatStyleIds.Reprisal)
        }, 1337, new(), 60);
        var snapshot = input.Character.CombatStyle!;
        Assert.Equal(CombatStyleIds.Reprisal, snapshot.RefinementId);
        Assert.Equal(.25, snapshot.Tuning.ReprisalAbsorbedDamageFraction);
        Assert.Equal(.10, snapshot.Tuning.ReprisalMaxHealthCapFraction);
        content.Validate(input);
        Assert.Throws<InvalidDataException>(() => content.Validate(input with
        {
            Character = input.Character with { CombatStyle = snapshot with
                { Tuning = snapshot.Tuning with { ReprisalAbsorbedDamageFraction = .5 } } }
        }));
        var runner = new IdleBattleRunner(content);
        var first = await runner.RunAsync(input);
        var second = await runner.RunAsync(input);
        Assert.Equal(HarnessJson.Hash(first), HarnessJson.Hash(second));
        Assert.Equal(CombatStyleIds.Reprisal, Assert.Single(first.Summary.CombatStyles!).RefinementId);
    }
}
