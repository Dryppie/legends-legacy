using BalanceHarness;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessActivityTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static OfflineContent Content => OfflineContent.ForTower(Root, TowerBundle.ReadSettings(Root));
    private static readonly Guid Owner = Guid.Parse("aef03768-c1c9-4a0d-9cf5-2f19c44b2dc6");

    [Fact]
    public async Task Joint_production_rewards_are_batch_independent_and_loss_mask_never_adds_rewards()
    {
        var model = new TowerActivityInventory(Root, Content);
        var full = await model.Rewards(Owner, "perfect", "region_01_area_04", 0, 8640, 10, default);
        var first = await model.Rewards(Owner, "perfect", "region_01_area_04", 0, 3000, 10, default);
        var rest = await model.Rewards(Owner, "perfect", "region_01_area_04", 3000, 8640, 10, default);
        Assert.Equal(HarnessJson.Hash(full.Equipment), HarnessJson.Hash(first.Equipment.Concat(rest.Equipment).ToArray()));
        var combined = first.Sigils.Concat(rest.Sigils).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Sum(p => p.Value));
        Assert.Equal(full.Sigils.OrderBy(p => p.Key), combined.OrderBy(p => p.Key));
        Assert.NotEmpty(full.Equipment);
        var losses = await model.Rewards(Owner, "four-of-five", "region_01_area_04", 0, 8640, 10, default);
        Assert.Equal(6912, losses.Victories);
        Assert.All(losses.Equipment, item => Assert.Contains(full.Equipment, original => original.Serialize() == item.Serialize()));
        Assert.All(losses.Sigils, pair => Assert.True(pair.Value <= full.Sigils[pair.Key]));
    }

    [Fact]
    public void Random_quest_armor_is_unconditioned_and_selection_retains_a_higher_budget_owned_item()
    {
        var content = Content;
        var model = new TowerActivityInventory(Root, content);
        var armor = Enumerable.Range(0, 32).Select(seed => model.QuestArmor(Owner, seed)).ToArray();
        Assert.True(armor.Select(a => a.State.DefinitionId).Distinct().Count() > 10);
        Assert.All(armor, a => Assert.Contains(a.EquipmentType, new[] { EquipmentType.Head, EquipmentType.Chest, EquipmentType.Legs }));
        var common = model.QuestMace(Owner);
        var stronger = EquipmentData.Create(common.EquipmentState.Reinforce(content.Equipment.Evaluator), content.Equipment.Evaluator);
        var selected = model.Select([common, stronger, armor[0]]);
        Assert.Contains(selected, e => ReferenceEquals(e.Data, stronger));
        Assert.DoesNotContain(selected, e => ReferenceEquals(e.Data, common));
        Assert.Equal(HarnessJson.Hash(selected), HarnessJson.Hash(model.Select([armor[0], stronger, common])));
    }

    [Fact]
    public void Activity_declaration_has_fixed_bounds_and_explicit_assumptions()
    {
        var plan = TowerActivityStudy.Read(Fixtures);
        Assert.Equal(86400, plan.Checkpoints.Last());
        Assert.Equal(12, plan.MaximumDungeonAttempts);
        Assert.Contains("not measured", plan.Assumptions);
    }

    private sealed class StudyFactAttribute : FactAttribute
    {
        public StudyFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_ACTIVITY")))
                Skip = "Requires frozen joint-activity owner; ordinary tests execute zero fights.";
        }
    }
    [StudyFact]
    public async Task Frozen_joint_activity_histories()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(14));
        await TowerActivityStudy.RunAsync(HarnessJson.Read<TowerActivityRequest>(Environment.GetEnvironmentVariable("LL_TOWER_ACTIVITY")!), deadline.Token);
    }
}
